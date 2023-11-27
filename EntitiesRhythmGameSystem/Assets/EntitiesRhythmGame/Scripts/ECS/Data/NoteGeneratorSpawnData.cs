using Unity.Entities;

namespace RhythmSystemEntities.ECS.Data
{
    //ノーツ生成用のエンティティの生成用データ
    public struct NoteGeneratorSpawnData : IComponentData
    {
        public Entity entityPrefab;
    }
}