using RhythmSystemEntities.ECS.Data;
using Unity.Entities;
using UnityEngine;

namespace RhythmSystemEntities.ECS.Authoring
{
    /// <summary>
    /// ノーツを生成するエンティティを生成する用のエンティティ変換用
    /// </summary>
    public class NoteGeneratorSpawnAuthoring : MonoBehaviour
    {
        [SerializeField] private GameObject generatorEntityPrefab;
        public class SpawnTestBaker : Baker<NoteGeneratorSpawnAuthoring>
        {
            public override void Bake(NoteGeneratorSpawnAuthoring authoring)
            {
                AddComponent(GetEntity(TransformUsageFlags.Dynamic),new NoteGeneratorSpawnData()
                {
                    entityPrefab = GetEntity(authoring.generatorEntityPrefab)
                });
            }
        }
    }
}