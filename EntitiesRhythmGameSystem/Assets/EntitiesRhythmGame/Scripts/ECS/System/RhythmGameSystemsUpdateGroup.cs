using Unity.Entities;

namespace RhythmSystemEntities.ECS.System
{
    /// <summary>
    /// ゲームシステムのUpdateで処理するもののグループ
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class RhythmGameSystemsUpdateGroup : ComponentSystemGroup
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            AddSystemToUpdateList(World.GetOrCreateSystem<NoteAllDataSpawnSystem>());
            AddSystemToUpdateList(World.GetOrCreateSystem<TimePresenterSystem>());
            AddSystemToUpdateList(World.GetOrCreateSystem<NotePresenterSystem>());
            AddSystemToUpdateList(World.GetOrCreateSystem<NoteJudgeSystem>());
            AddSystemToUpdateList(World.GetOrCreateSystem<NoteOptionPresenterSystem>());
            
        }

    }
}