using RhythmSystemEntities.Data;
using RhythmSystemEntities.ECS.Aspect;
using RhythmSystemEntities.ECS.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace RhythmSystemEntities.ECS.System
{
    /// <summary>
    /// ノーツやPresenter,Generatorを生成するSystem
    /// </summary>
    [DisableAutoCreation]
    public partial class NoteAllDataSpawnSystem : SystemBase
    {
        //生成時に使うデータ
        public struct GenerateData
        {
            //使い終わったかどうか
            public bool isUsed;
            //MusicID
            public int musicID;
            //曲とのズレを修正するもの(sec)
            public float offsetSec;
            //ポジションのオフセット
            public float2 posOffset;
            //ポジションの調整(間隔)
            public float2 posAdjustor;
            //BPMChangeの配列データ
            public NativeArray<BPMChange> bpmChanges;
            //NotePropertyTempの配列データ
            public NativeArray<NotePropertyTemp> notePropertyTemps;
            //オプションデータ
            public NoteOptionData noteOptionData;
        }

        //生成時に使うデータ
        private GenerateData generateDataTemp;

        protected override void OnCreate()
        {
            base.OnCreate();
            //仮データを入れておく
            generateDataTemp = new GenerateData()
            {
                isUsed = true
            };
        }

        protected override void OnUpdate()
        {
            //データが使われていたらスキップ
            if(generateDataTemp.isUsed) return;
            
            //EntityCommandBufferSingletonを取得してAllocatorセット
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            ecb.SetAllocator(Allocator.TempJob);
            
            //Presenterの生成Jobを発行
            Dependency = new GeneratePresenterEntityJob
            {
                ecb = ecb.CreateCommandBuffer(World.Unmanaged),
                generateData = generateDataTemp,
            }.Schedule(Dependency);
            Dependency.Complete();
            
            //Notesを生成するJobを発行
            Dependency = new GenerateNoteEntitiesJob()
            {
                ecb = ecb.CreateCommandBuffer(World.Unmanaged),
                generateData = generateDataTemp,
            }.Schedule(Dependency);
            Dependency.Complete();

            //終わったら破棄
            generateDataTemp.notePropertyTemps.Dispose();
            generateDataTemp.bpmChanges.Dispose();
            //使ったフラグ
            generateDataTemp.isUsed = true;
        }

        /// <summary>
        /// これを外部から呼ぶと全て動く
        /// </summary>
        /// <param name="generateData">生成時に使うメインのデータ</param>
        public void Spawn(GenerateData generateData)
        {
            //EntityCommandBufferを生成して取得
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(World.Unmanaged);
            //NoteGeneratorを生成するJobを発行
            Dependency = new NoteGeneratorSpawnJob
            {
                ecb = ecb
            }.Schedule(Dependency);
            Dependency.Complete();

            //生成に使うデータを入れる
            generateDataTemp = generateData;
        }
    }
    
    /// <summary>
    /// NoteGeneratorを生成するJob
    /// </summary>
    [BurstCompile]
    public partial struct NoteGeneratorSpawnJob : IJobEntity
    {
        public EntityCommandBuffer ecb;
        
        [BurstCompile]
        private void Execute(NoteGeneratorSpawnAspect noteGeneratorSpawnAspect)
        {
            //生成
            ecb.Instantiate(noteGeneratorSpawnAspect.GetEntity());
        }
    }

    /// <summary>
    /// Presenterを生成するJob
    /// </summary>
    [BurstCompile]
    public partial struct GeneratePresenterEntityJob : IJobEntity
    {
        public EntityCommandBuffer ecb;
        public NoteAllDataSpawnSystem.GenerateData generateData;
        
        [BurstCompile]
        private void Execute(NoteGeneratorAspect noteGeneratorAspect)
        {
            //保険
            //生成終わっていたらスキップ
            if (noteGeneratorAspect.GetIsInit()) return;
            
            //Generatorの初期化
            noteGeneratorAspect.Init(generateData.musicID,generateData.notePropertyTemps,generateData.bpmChanges);
            
            //Presenterの生成
            Entity entity = ecb.Instantiate(noteGeneratorAspect.GetNotePresenterDataEntity());
                            
            //MusicIDData
            MusicIDData musicIDData = new MusicIDData()
            {
                musicID = generateData.musicID,
            };
                
            //オプション
            NoteOptionData noteOptionData = generateData.noteOptionData;
            
            //PresenterのData生成
            NotePresenterData data = new NotePresenterData();
            data.nowSec = 0;
            data.blobAssetReference = noteGeneratorAspect.GetBPMChangesBlobRef();
            
            ecb.AddComponent(entity,data);
            ecb.AddComponent(entity,musicIDData);
            ecb.AddComponent(entity,noteOptionData);
        }
    }
    
    /// <summary>
    /// ノーツの生成Job
    /// </summary>
    [BurstCompile]
    public partial struct GenerateNoteEntitiesJob : IJobEntity
    {
        public EntityCommandBuffer ecb;
        public NoteAllDataSpawnSystem.GenerateData generateData;
        
        [BurstCompile]
        private void Execute(NoteGeneratorAspect noteGeneratorAspect)
        {
            //既に初期化生成済みならスキップ
            if (noteGeneratorAspect.GetIsInit()) return;
            
            //曲IDが違っていたらスキップ
            if (generateData.musicID != noteGeneratorAspect.GetMusicID()) return;

            //NotePropertyTempsのBlobAssetのrefを取得
            BlobAssetReference<NotePropertyTempsBlob> notePropertyTempsBlobRef =
                noteGeneratorAspect.GetNotePropertyTempsBlobRef();

            //ロングノーツのカウント
            int longNoteCount = 0;
            //NotePropertyTempsの長さ分生成する
            for (int i = 0; i < notePropertyTempsBlobRef.Value.notePropertyTemps.Length; i++)
            {
                Entity entity;
                int beginEdgeID = -1;
                int endEdgeID = -1;

                //始まりと終わりのBeatをSecに変換
                float beginSec = ToSec(
                    notePropertyTempsBlobRef.Value.notePropertyTemps[i].beginBeat,
                    noteGeneratorAspect.GetBPMChangesBlobRef());
                float endSec = ToSec(
                    notePropertyTempsBlobRef.Value.notePropertyTemps[i].endBeat,
                    noteGeneratorAspect.GetBPMChangesBlobRef());

                //ノーツの種類
                var noteType = notePropertyTempsBlobRef.Value.notePropertyTemps[i].noteType;
                
                //MusicIDData
                MusicIDData musicIDData = new MusicIDData()
                {
                    musicID = generateData.musicID,
                };
                
                //オプション
                NoteOptionData noteOptionData = generateData.noteOptionData;

                //ノーツの種類で対応したEntityを生成
                if (noteType == NoteComponentData.NoteType.Single)
                {
                    entity = ecb.Instantiate(noteGeneratorAspect.GetSingleNoteEntity());
                }
                else
                {
                    //ロングノーツは作るものがいくつかある
                    entity = ecb.Instantiate(noteGeneratorAspect.GetLongNoteEntity());

                    //ロングノーツの始点と終点エンティティ
                    Entity beginEdgeEntity, endEdgeEntity;
                    beginEdgeEntity = ecb.Instantiate(noteGeneratorAspect.GetLongNoteEdgeEntity());
                    endEdgeEntity = ecb.Instantiate(noteGeneratorAspect.GetLongNoteEdgeEntity());

                    //EdgeID設定
                    beginEdgeID = longNoteCount * 2;
                    endEdgeID = longNoteCount * 2 + 1;

                    //始点データ
                    LongNoteEdgeData longNoteBeginEdgeData = new LongNoteEdgeData()
                    {
                        edgeID = beginEdgeID,
                        timingData = new LongNoteEdgeData.TimingData()
                        {
                            beat = notePropertyTempsBlobRef.Value.notePropertyTemps[i].beginBeat,
                            offsetBeat = ToBeat(generateData.offsetSec, noteGeneratorAspect.GetBPMChangesBlobRef()),
                        },
                        lane = notePropertyTempsBlobRef.Value.notePropertyTemps[i].lane,
                        isExecuted = false,
                        nowBeat = 0,
                        posOffset = generateData.posOffset,
                        posAdjustor = generateData.posAdjustor,
                    };
                    
                    //終点データ
                    LongNoteEdgeData longNoteEndEdgeData = new LongNoteEdgeData()
                    {
                        edgeID = endEdgeID,
                        timingData = new LongNoteEdgeData.TimingData()
                        {
                            beat = notePropertyTempsBlobRef.Value.notePropertyTemps[i].endBeat,
                            offsetBeat = ToBeat(generateData.offsetSec, noteGeneratorAspect.GetBPMChangesBlobRef()),
                        },
                        lane = notePropertyTempsBlobRef.Value.notePropertyTemps[i].lane,
                        isExecuted = false,
                        nowBeat = 0,
                        posOffset = generateData.posOffset,
                        posAdjustor = generateData.posAdjustor,
                    };

                    //始点終点のオプションデータ
                    NoteOptionData beginEdgeOptionData = noteOptionData;
                    NoteOptionData endEdgeOptionData = noteOptionData;
                    
                    //始点終点のMusicIDData
                    MusicIDData beginMusicIDData = musicIDData;
                    MusicIDData endMusicIDData = musicIDData;
                    
                    //それぞれにデータをセット
                    ecb.AddComponent(beginEdgeEntity,longNoteBeginEdgeData);
                    ecb.AddComponent(beginEdgeEntity,beginEdgeOptionData);
                    ecb.AddComponent(beginEdgeEntity,beginMusicIDData);
                    ecb.AddComponent(endEdgeEntity,longNoteEndEdgeData);
                    ecb.AddComponent(endEdgeEntity,endEdgeOptionData);
                    ecb.AddComponent(endEdgeEntity,endMusicIDData);

                    //カウントアップ
                    longNoteCount++;
                }
                                    
                //メインのData
                NoteComponentData data = new NoteComponentData()
                {
                    noteType = noteType,
                    timingData = new NoteComponentData.TimingData()
                    {
                        beginBeat = notePropertyTempsBlobRef.Value.notePropertyTemps[i].beginBeat,
                        beginSec = beginSec,
                        endBeat = notePropertyTempsBlobRef.Value.notePropertyTemps[i].endBeat,
                        endSec = endSec,
                        offsetBeat = ToBeat(generateData.offsetSec,noteGeneratorAspect.GetBPMChangesBlobRef()),
                        offsetSec = generateData.offsetSec,
                    },
                    longNoteEdgeIDData = new NoteComponentData.LongNoteEdgeIDData()
                    {
                        beginID = beginEdgeID,
                        endID = endEdgeID
                    },
                    lane = notePropertyTempsBlobRef.Value.notePropertyTemps[i].lane,
                    nowBeat = 0,
                    nowSec = 0,
                    isProcessing = false,
                    isExecuted = false,
                    posOffset = generateData.posOffset,
                    posAdjustor = generateData.posAdjustor,
                };
                
                //データをセット
                ecb.AddComponent(entity,data);
                ecb.AddComponent(entity,noteOptionData);
                ecb.AddComponent(entity,musicIDData);
            }

            //generate終わり
            noteGeneratorAspect.InitComplete();
        }
        
        //beat -> sec
        [BurstCompile]
        public float ToSecWithFixedBPM(float beat,float bpm)
        {
            float bps = (bpm / 60);
            return beat / bps;
        }
        
        //sec -> beat
        [BurstCompile]
        public static float ToBeatWithFixedBPM(float sec,float bpm)
        {
            float bps = (bpm / 60);
            return sec * bps;
        }
        
        //BPM変化情報を基にbeat -> sec
        [BurstCompile]
        public float ToSec(float nowBeat,BlobAssetReference<BPMChangesBlob> bpmChanges)
        {
            //累計秒数
            float accumulatedSec = 0f;
            //BPM変化index
            int index = 0;
            //既に変化済みの回数
            int alreadyChangedCount = 0;
            //変化済み回数カウント
            for (int i = 0; i < bpmChanges.Value.bpmChanges.Length; i++)
            {
                if (bpmChanges.Value.bpmChanges[i].executeBeat <= nowBeat)
                {
                    alreadyChangedCount++;
                }
                else
                {
                    break;
                }
            }
            
            //変換するbeatの直前にあるBPM変化までのsecを求める
            while(index < alreadyChangedCount - 1)
            {
                //変化済みの回数分秒数を足す
                accumulatedSec += 
                    ToSecWithFixedBPM(
                        bpmChanges.Value.bpmChanges[index + 1].executeBeat - bpmChanges.Value.bpmChanges[index].executeBeat, 
                        bpmChanges.Value.bpmChanges[index].afterBPM
                    );
                index++;
            }
            
            //残りのbeat分を足す
            accumulatedSec += 
                ToSecWithFixedBPM(
                    nowBeat - bpmChanges.Value.bpmChanges[index].executeBeat, 
                    bpmChanges.Value.bpmChanges[index].afterBPM
                );
            
            return accumulatedSec;
        }
        
        [BurstCompile]
        //BPM変化情報を基にsec -> beat
        private float ToBeat(float nowSec,BlobAssetReference<BPMChangesBlob> bpmChanges)
        {
            //累計秒数
            float accumulatedSec = 0f;
            //BPM変化index
            int index = 0;
            //既に変化済みの回数
            int alreadyChangedCount = bpmChanges.Value.bpmChanges.Length;

            //最後から1つ前のBPM変化までループ
            while(index < alreadyChangedCount - 1)
            {                      
                //index回目のBPM変化地点での秒数
                float tmpSec = accumulatedSec;
                
                //次(index + 1)のテンポ変化のタイミング(秒)の計算
                accumulatedSec += 
                    ToSecWithFixedBPM(
                        bpmChanges.Value.bpmChanges[index + 1].executeBeat - bpmChanges.Value.bpmChanges[index].executeBeat,
                        bpmChanges.Value.bpmChanges[index].afterBPM
                    );
                
                if(accumulatedSec >= nowSec)
                {
                    //次のBPM変化タイミングが変換するsecを超えた場合、「超える直前のBPM変化があるbeat + 残りのbeat」を返す
                    return bpmChanges.Value.bpmChanges[index].executeBeat + 
                           ToBeatWithFixedBPM(
                               nowSec - tmpSec,
                               bpmChanges.Value.bpmChanges[index].afterBPM
                           );
                }
                
                index++;

            }
            //変換するsecが最後のBPM変化よりも後にある場合、「最後のBPM変化があるbeat + 残りのbeat」を返す
            return bpmChanges.Value.bpmChanges[alreadyChangedCount - 1].executeBeat + 
                   ToBeatWithFixedBPM(
                       nowSec - accumulatedSec,
                       bpmChanges.Value.bpmChanges[alreadyChangedCount - 1].afterBPM
                   );
        }
    }
}