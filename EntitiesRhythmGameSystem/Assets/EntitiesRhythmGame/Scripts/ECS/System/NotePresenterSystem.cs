using RhythmSystemEntities.Data;
using RhythmSystemEntities.ECS.Aspect;
using RhythmSystemEntities.ECS.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace RhythmSystemEntities.ECS.System
{
    /// <summary>
    /// presenter->ノーツのデータデリバリーシステム
    /// </summary>
    [BurstCompile]
    [UpdateAfter(typeof(TimePresenterSystem))]
    [DisableAutoCreation]
    public partial struct NotePresenterSystem : ISystem
    {
        /// <summary>
        /// JobとJob間で値のやりとりをするときに一時的に使う
        /// </summary>
        public struct NotePresenterDataTemp
        {
            //MusicIDのこと
            public int id;
            //今の時間(sec)
            public float nowSec;
            //BPMChangesのBlobAssetのRef
            public BlobAssetReference<BPMChangesBlob> bpmChangesBlob;
            
            //オプションデータ
            public NoteOptionData noteOptionData;

            //使えるデータかどうか
            public bool isUsable;
        } 
            
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            
            //一時保存データを作成
            NativeArray<NotePresenterDataTemp> temps = new NativeArray<NotePresenterDataTemp>(GameSettings.musicNumMax,Allocator.TempJob);
            //一時データに必要なデータを入れるJobを発行
            state.Dependency = new NotePresenterDataGetJob() {presenterDatTemps = temps}.Schedule(state.Dependency);
            state.Dependency.Complete();

            //一時データを使ってノーツのデータを更新するJobを発行
            state.Dependency = new NoteNowSecBeatUpdateJob() {temps = temps}.ScheduleParallel(state.Dependency);
            state.Dependency.Complete();

            //一時データを使ってノーツ端のデータを更新するJobを発行
            state.Dependency = new NoteEdgeNowSecBeatUpdateJob() {temps = temps}.ScheduleParallel(state.Dependency);
            state.Dependency.Complete();

            //一時データを使ってノーツのオプションデータを更新するJobを発行
            state.Dependency = new NoteOptionDataUpdateJob() {temps = temps}.ScheduleParallel(state.Dependency);
            state.Dependency.Complete();

            //終わったので破棄
            temps.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            //BlobAssetはシーンアンロード時に自動で破棄されるらしい
            
            // state.Dependency = new NotePresenterDisposeJob().ScheduleParallel(state.Dependency);
            // state.Dependency.Complete();
        }

    }

    /// <summary>
    /// 一時データに必要なデータを入れるJob
    /// </summary>
    [BurstCompile]
    public partial struct NotePresenterDataGetJob : IJobEntity
    {
        public NativeArray<NotePresenterSystem.NotePresenterDataTemp> presenterDatTemps;
        [BurstCompile]
        private void Execute(NotePresenterAspect notePresenterAspect)
        {
            //曲の数分(配列の長さ分)繰り返し
            for (int i = 0; i < presenterDatTemps.Length; i++)
            {
                //i == MusicIDで紐づいている
                if (i == notePresenterAspect.GetMusicID())
                {
                    //仮データ作成して配列に格納
                    presenterDatTemps[i] = new NotePresenterSystem.NotePresenterDataTemp()
                    {
                        id = notePresenterAspect.GetMusicID(),
                        nowSec = notePresenterAspect.GetNowSec(),
                        bpmChangesBlob = notePresenterAspect.GetBPMChangesBlob(),
                        noteOptionData = notePresenterAspect.GetOptionData(),
                        isUsable = true
                    };
                }
                else
                {
                    //仮データ(使わない)を作成して配列に格納
                    presenterDatTemps[i] = new NotePresenterSystem.NotePresenterDataTemp()
                    {
                        id = 9999,
                        nowSec = 9999,
                        bpmChangesBlob = new BlobAssetReference<BPMChangesBlob>(),
                        isUsable = false
                    };
                }
            }
        }
    }

    /// <summary>
    /// 一時データを使ってノーツのデータを更新するJob
    /// </summary>
    [BurstCompile]
    public partial struct NoteNowSecBeatUpdateJob : IJobEntity
    {
        public NativeArray<NotePresenterSystem.NotePresenterDataTemp> temps;
        [BurstCompile]
        private void Execute(NoteAspect noteAspect)
        {
            //配列の長さ文繰り返し
            foreach (var value in temps)
            {
                //IDがノーツのMusicIDと合わなかったらスキップ
                if (value.id != noteAspect.GetMusicID()) continue;
                //データが使えないデータならスキップ
                if (!value.isUsable) continue;
                
                //今の時間(sec)
                float nowSec = value.nowSec;
                //今の時間(beat)に変換
                float nowBeat = ToBeat(nowSec, value.bpmChangesBlob);
                    
                //今の時間を更新
                noteAspect.SetNowSec(nowSec);
                noteAspect.SetNowBeat(nowBeat);
            }
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
        
        [BurstCompile]
        //beat -> sec
        private float ToSecWithFixedBPM(float beat,float bpm)
        {
            float bps = (bpm / 60);
            return beat / bps;
        }
        
        [BurstCompile]
        //sec -> beat
        private float ToBeatWithFixedBPM(float sec,float bpm)
        {
            float bps = (bpm / 60);
            return sec * bps;
        }
    }

    /// <summary>
    /// 一時データを使ってノーツ端のデータを更新するJob
    /// </summary>
    [BurstCompile]
    public partial struct NoteEdgeNowSecBeatUpdateJob : IJobEntity
    {
        public NativeArray<NotePresenterSystem.NotePresenterDataTemp> temps;
        [BurstCompile]
        private void Execute(LongNoteEdgeAspect longNoteEdgeAspect)
        {
            //配列の長さ文繰り返し
            foreach (var value in temps)
            {
                //IDがノーツのMusicIDと合わなかったらスキップ
                if (value.id != longNoteEdgeAspect.GetMusicID()) continue;
                //データが使えないデータならスキップ
                if (!value.isUsable) continue;
                
                //今の時間(sec)
                float nowSec = value.nowSec;
                //今の時間(beat)に変換
                float nowBeat = ToBeat(nowSec, value.bpmChangesBlob);
                    
                //今の時間を更新
                longNoteEdgeAspect.SetNowBeat(nowBeat);
            }
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
        
        [BurstCompile]
        //beat -> sec
        private float ToSecWithFixedBPM(float beat,float bpm)
        {
            float bps = (bpm / 60);
            return beat / bps;
        }
        
        [BurstCompile]
        //sec -> beat
        private float ToBeatWithFixedBPM(float sec,float bpm)
        {
            float bps = (bpm / 60);
            return sec * bps;
        }
    }
    
    /// <summary>
    /// 一時データを使ってノーツのオプションデータを更新するJob
    /// </summary>
    [BurstCompile]
    public partial struct NoteOptionDataUpdateJob : IJobEntity
    {
        public NativeArray<NotePresenterSystem.NotePresenterDataTemp> temps;
        [BurstCompile]
        private void Execute(NoteOptionAspect noteOptionAspect)
        {
            //配列の長さ文繰り返し
            foreach (var value in temps)
            {
                //IDがノーツのMusicIDと合わなかったらスキップ
                if (value.id != noteOptionAspect.GetMusicID()) continue;
                //データが使えないデータならスキップ
                if (!value.isUsable) continue;

                //オプション更新
                noteOptionAspect.SetScrollSpeed(value.noteOptionData.scrollSpeed);
                noteOptionAspect.SetPlayerInputOffset(value.noteOptionData.playerInputOffset);
            }
        }
    }

    /// <summary>
    /// 使うかわからないけどPresenterのDispose用Job
    /// </summary>
    [BurstCompile]
    public partial struct NotePresenterDisposeJob : IJobEntity
    {
        [BurstCompile]
        private void Execute(NotePresenterAspect notePresenterAspect)
        {
            notePresenterAspect.Dispose();
        }
    }
}