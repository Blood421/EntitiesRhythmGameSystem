using RhythmSystemEntities.ECS.Aspect;
using Unity.Burst;
using Unity.Entities;

namespace RhythmSystemEntities.ECS.System
{
    /// <summary>
    /// プレゼンターにオプションデータをデリバリーするシステム
    /// </summary>
    [DisableAutoCreation]
    [UpdateBefore(typeof(NotePresenterSystem))]
    public partial class NoteOptionPresenterSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            //特に使わない
            Enabled = false;
        }

        //外部から設定時に呼ぶ
        public void SetNoteOption(int musicID, float scrollSpeed,float playerInputOffset)
        {
            //プレゼンターのデータにセットするJobの発行
            Dependency = new SetNoteOptionJob()
            {
                musicID = musicID,
                scrollSpeed = scrollSpeed,
                playerInputOffset = playerInputOffset
            }.ScheduleParallel(Dependency);
            Dependency.Complete();
        }
    }
    /// <summary>
    /// プレゼンターのデータにセットするJobの発行
    /// </summary>
    [BurstCompile]
    public partial struct SetNoteOptionJob : IJobEntity
    {
        public int musicID;
        public float scrollSpeed;
        public float playerInputOffset;
        
        [BurstCompile]
        private void Execute(NotePresenterAspect notePresenterAspect)
        {
            //IDが違ったらスキップ
            if (musicID != notePresenterAspect.GetMusicID()) return;
            //セット
            notePresenterAspect.SetScrollSpeed(scrollSpeed);
            notePresenterAspect.SetPlayerInputOffset(playerInputOffset);
        }
    }
}