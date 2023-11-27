using RhythmSystemEntities.ECS.Data;
using Unity.Entities;
using UnityEngine;

namespace RhythmSystemEntities.ECS.Authoring
{
    /// <summary>
    /// ノーツ生成設定のエンティティ変換用
    /// </summary>
    public class NoteGeneratorAuthoring : MonoBehaviour
    {
        [SerializeField,Tooltip("シングルノーツのEntityPrefab")] private GameObject singleNotePrefab;
        [SerializeField,Tooltip("ロングノーツのEntityPrefab")] private GameObject longNotePrefab;
        [SerializeField,Tooltip("ロングノーツ端のEntityPrefab")] private GameObject longNoteEdgePrefab;
        [SerializeField,Tooltip("ノーツのpresenterのEntityPrefab")] private GameObject notePresenterDataPrefab;
        public class NoteGeneratorBaker : Baker<NoteGeneratorAuthoring>
        {
            public override void Bake(NoteGeneratorAuthoring authoring)
            {
                NoteGeneratorData noteGeneratorData = new NoteGeneratorData();
                //MusicIDはあとで設定されるから仮
                noteGeneratorData.musicID = 0;
                //ノーツ生成用のエンティティ設定
                noteGeneratorData.singleNoteEntity = GetEntity(authoring.singleNotePrefab);
                noteGeneratorData.longNoteEntity = GetEntity(authoring.longNotePrefab);
                noteGeneratorData.longNoteEdgeEntity = GetEntity(authoring.longNoteEdgePrefab);
                noteGeneratorData.notePresenterDataEntity = GetEntity(authoring.notePresenterDataPrefab);
                noteGeneratorData.isInit = false;

                //コンポーネントを付ける
                AddComponent(GetEntity(TransformUsageFlags.Dynamic),noteGeneratorData);
            }
        }
    }
}