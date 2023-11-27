using Unity.Entities;

namespace RhythmSystemEntities.ECS.Data
{
    //ノーツのオプションデータ
    public struct NoteOptionData : IComponentData
    {
        public float scrollSpeed;
        public float playerInputOffset;
    }
}