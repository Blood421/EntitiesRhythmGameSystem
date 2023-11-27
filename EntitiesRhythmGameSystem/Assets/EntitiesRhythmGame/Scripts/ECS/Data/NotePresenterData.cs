using Unity.Entities;

namespace RhythmSystemEntities.ECS.Data
{
    /// <summary>
    /// MonoからのECSのノーツ橋渡し用のデータ
    /// </summary>
    public struct NotePresenterData : IComponentData
    {
        //今の時間(sec)
        public float nowSec;
        //BPMChangesのBlobAssetのRef
        public BlobAssetReference<BPMChangesBlob> blobAssetReference;
    }
}