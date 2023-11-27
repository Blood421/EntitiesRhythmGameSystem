using RhythmSystemEntities.ECS.Data;
using Unity.Entities;

namespace RhythmSystemEntities.ECS.Aspect
{
    /// <summary>
    /// ノーツ生成するエンティティを保持するエンティティのアスペクト
    /// </summary>
    public readonly partial struct NoteGeneratorSpawnAspect : IAspect
    {
        private readonly Entity entity;
        private readonly RefRO<NoteGeneratorSpawnData> noteGeneratorSpawnDataRO;

        public Entity GetEntity() => noteGeneratorSpawnDataRO.ValueRO.entityPrefab;
    }
}