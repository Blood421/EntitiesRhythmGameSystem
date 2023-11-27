using RhythmSystemEntities.ECS.Aspect;
using Unity.Burst;
using Unity.Entities;

namespace RhythmSystemEntities.ECS.System
{
    /// <summary>
    /// ノーツを移動させるシステム
    /// </summary>
    [BurstCompile]
    [DisableAutoCreation]
    [UpdateAfter(typeof(LongNoteEdgeExecuteSystem))]
    public partial struct NoteViewUpdateSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            //見た目更新Job発行
            //いつ終わってもいい
            new NoteViewUpdateJob().ScheduleParallel();
            new LongNoteEdgeViewUpdateJob().ScheduleParallel();
        }
    }
    
    /// <summary>
    /// ノーツの見た目を更新するJob
    /// </summary>
    [BurstCompile]
    public partial struct NoteViewUpdateJob : IJobEntity
    {
        [BurstCompile]
        private void Execute(NoteAspect noteAspect)
        {
            //ノーツの見た目の更新
            noteAspect.ViewUpdate();
        }
    }
    /// <summary>
    /// ロングノーツの端の見た目を更新するJob
    /// </summary>
    [BurstCompile]
    public partial struct LongNoteEdgeViewUpdateJob : IJobEntity
    {
        [BurstCompile]
        private void Execute(LongNoteEdgeAspect longNoteEdgeAspect)
        {
            //ノーツの見た目の更新
            longNoteEdgeAspect.ViewUpdate();
        }
    }
}