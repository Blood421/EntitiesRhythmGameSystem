using RhythmSystemEntities.Data;
using Unity.Entities;

namespace RhythmSystemEntities.ECS.Data
{
    /// <summary>
    /// ノーツ生成用エンティティのデータ
    /// </summary>
    public struct NoteGeneratorData : IComponentData
    {
        //ノーツ生成処理が終わっているかどうか
        public bool isInit;
        //シングルノーツのEntity
        public Entity singleNoteEntity;
        //ロングノーツのEntity
        public Entity longNoteEntity;
        //ロングノーツ端のEntity
        public Entity longNoteEdgeEntity;
        //NotePresenterのEntity
        public Entity notePresenterDataEntity;
        //MusicID
        public int musicID;
        //BPMChangesのBlobAssetのRef
        public BlobAssetReference<BPMChangesBlob> bpmChangesBlobRef;
        //NotePropertyTempsのBlobAssetのRef
        public BlobAssetReference<NotePropertyTempsBlob> notePropertyTempsBlobRef;
    }
    
    //BPMChangesのBlobを生成するときに使う
    public struct BPMChangesBlob
    {
        public BlobArray<BPMChange> bpmChanges;
    }
    
    //NotePropertyTempsのBlobを生成するときに使う
    public struct NotePropertyTempsBlob
    { 
        public BlobArray<NotePropertyTemp> notePropertyTemps;
    }
}