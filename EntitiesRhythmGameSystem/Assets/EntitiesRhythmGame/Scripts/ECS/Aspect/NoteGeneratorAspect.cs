using RhythmSystemEntities.Data;
using RhythmSystemEntities.ECS.Data;
using Unity.Collections;
using Unity.Entities;

namespace RhythmSystemEntities.ECS.Aspect
{
    /// <summary>
    /// ノーツ生成用エンティティのアスペクト
    /// </summary>
    public readonly partial struct NoteGeneratorAspect : IAspect
    {
        //エンティティ
        private readonly Entity entity;
        //NoteGeneratorのData
        private readonly RefRW<NoteGeneratorData> noteGeneratorDataRW;
        
        /// <summary>
        /// Generator生成後の初期化
        /// </summary>
        /// <param name="musicID">MusicID</param>
        /// <param name="notePropertyTemps">NotePropertyTempのNativeArray</param>
        /// <param name="bpmChanges">BPMChangeのNativeArray</param>
        public void Init(int musicID , NativeArray<NotePropertyTemp> notePropertyTemps,NativeArray<BPMChange> bpmChanges)
        {
            //曲ID
            noteGeneratorDataRW.ValueRW.musicID = musicID;
            
            //BPMChangesとNotePropertyTempsのブロブアセット(Array)の生成
            BlobBuilder builder1 = new BlobBuilder(Allocator.Temp);
            BlobBuilder builder2 = new BlobBuilder(Allocator.Temp);
                
            ref BPMChangesBlob bpmChangesBlobRefData = ref builder1.ConstructRoot<BPMChangesBlob>();
            BlobBuilderArray<BPMChange> bpmChangesArrayBuilder = builder1.Allocate(
                ref bpmChangesBlobRefData.bpmChanges,
                bpmChanges.Length
                );

            for (int i = 0; i < bpmChangesArrayBuilder.Length; i++)
            {
                bpmChangesArrayBuilder[i] = new BPMChange()
                {
                    executeBeat = bpmChanges[i].executeBeat,
                    afterBPM = bpmChanges[i].afterBPM
                };
            }
                
            ref NotePropertyTempsBlob notePropertyTempsBlobRefData = ref builder2.ConstructRoot<NotePropertyTempsBlob>();
            BlobBuilderArray<NotePropertyTemp> notePropertyTempsArrayBuilder = builder2.Allocate(
                ref notePropertyTempsBlobRefData.notePropertyTemps,
                notePropertyTemps.Length
                );

            for (var i = 0; i < notePropertyTempsArrayBuilder.Length; i++)
            { 
                notePropertyTempsArrayBuilder[i] = new NotePropertyTemp() 
                {
                    noteType = notePropertyTemps[i].noteType,
                    beginBeat = notePropertyTemps[i].beginBeat,
                    endBeat = notePropertyTemps[i].endBeat,
                    lane = notePropertyTemps[i].lane
                };
            }
                
            var resultBPMChanges = builder1.CreateBlobAssetReference<BPMChangesBlob>(Allocator.Persistent);
            var resultNotePropertyTemps = builder2.CreateBlobAssetReference<NotePropertyTempsBlob>(Allocator.Persistent);
            //生成したものを格納
            noteGeneratorDataRW.ValueRW.bpmChangesBlobRef = resultBPMChanges;
            noteGeneratorDataRW.ValueRW.notePropertyTempsBlobRef = resultNotePropertyTemps;
            //builderを破棄
            builder1.Dispose();
            builder2.Dispose();
        }
        
        /// <summary>
        /// ノーツの生成が完了した
        /// </summary>
        public void InitComplete() => noteGeneratorDataRW.ValueRW.isInit = true;
        
        /// <summary>
        /// musicIDの取得
        /// </summary>
        /// <returns>ノーツのMusicID</returns>
        public int GetMusicID() => noteGeneratorDataRW.ValueRO.musicID;

        /// <summary>
        /// BPMChangesのBlobAssetのRefを取得
        /// </summary>
        /// <returns>BPMChangesのBlobAssetのRef</returns>
        public BlobAssetReference<BPMChangesBlob> GetBPMChangesBlobRef() => noteGeneratorDataRW.ValueRO.bpmChangesBlobRef;

        /// <summary>
        /// NotePropertyTempsのBlobAssetのRefを取得
        /// </summary>
        /// <returns>NotePropertyTempsのBlobAssetのRef</returns>
        public BlobAssetReference<NotePropertyTempsBlob> GetNotePropertyTempsBlobRef() => noteGeneratorDataRW.ValueRO.notePropertyTempsBlobRef;
        
        
        /// <summary>
        /// シングルノーツのEntityの取得
        /// </summary>
        /// <returns>シングルノーツのEntity</returns>
        public Entity GetSingleNoteEntity() => noteGeneratorDataRW.ValueRO.singleNoteEntity;
        
        /// <summary>
        /// ロングノーツのEntityの取得
        /// </summary>
        /// <returns>ロングノーツのEntity</returns>
        public Entity GetLongNoteEntity() => noteGeneratorDataRW.ValueRO.longNoteEntity;
        
        /// <summary>
        /// ロングノーツ端のEntityの取得
        /// </summary>
        /// <returns>ロングノーツ端のEntity</returns>
        public Entity GetLongNoteEdgeEntity() => noteGeneratorDataRW.ValueRO.longNoteEdgeEntity;

        /// <summary>
        /// NotePresenterのEntityの取得
        /// </summary>
        /// <returns>NotePresenterのEntity</returns>
        public Entity GetNotePresenterDataEntity() => noteGeneratorDataRW.ValueRO.notePresenterDataEntity;
        
        /// <summary>
        /// ノーツの生成が終わっているかどうかの取得
        /// </summary>
        /// <returns>ノーツの生成が終わっているかどうか</returns>
        public bool GetIsInit() => noteGeneratorDataRW.ValueRO.isInit;

    }
}