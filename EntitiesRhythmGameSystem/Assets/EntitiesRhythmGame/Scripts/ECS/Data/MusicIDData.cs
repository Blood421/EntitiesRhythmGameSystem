using Unity.Entities;

namespace RhythmSystemEntities.ECS.Data
{
    /// <summary>
    /// 曲IDデータ
    /// </summary>
    public struct MusicIDData : IComponentData
    {
        public int musicID;
    }
}