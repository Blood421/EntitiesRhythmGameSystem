using Unity.Entities;

namespace RhythmSystemEntities.ECS.System
{
    /// <summary>
    /// ゲームシステムのLateUpdateで処理するもののグループ
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class RhythmGameSystemsLateUpdateGroup : ComponentSystemGroup
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            AddSystemToUpdateList(World.GetOrCreateSystem<LongNoteEdgeExecuteSystem>());
            AddSystemToUpdateList(World.GetOrCreateSystem<NoteViewUpdateSystem>());
        }
    }
}