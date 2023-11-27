using RhythmSystemEntities.Data;
using RhythmSystemEntities.ECS.Aspect;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace RhythmSystemEntities.ECS.System
{
    /// <summary>
    /// Mono側からECS側にデータを持っていくためのSystem
    /// </summary>
    [DisableAutoCreation]
    [UpdateAfter(typeof(NoteAllDataSpawnSystem))]
    public partial class TimePresenterSystem : SystemBase
    {
        //今の時間(sec)を入れておいてPresenterを更新させるデータ
        private NativeArray<float> nowSecPresentData;
        
        protected override void OnCreate()
        {
            base.OnCreate();
            //データの生成と初期化
            nowSecPresentData = new NativeArray<float>(GameSettings.musicNumMax,Allocator.Persistent);
            for (int i = 0; i < nowSecPresentData.Length; i++)
            {
                nowSecPresentData[i] = 0;
            }
        }

        protected override void OnUpdate()
        {
            //NotePresenterのnowSecを更新するJobを発行
            Dependency = new NowSecPresentJob()
            {
                nowSecPresentData = nowSecPresentData
            }.Schedule(Dependency);
            Dependency.Complete();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            //データの破棄
            nowSecPresentData.Dispose();
        }

        /// <summary>
        /// 今の時間(sec)を更新するときに使う
        /// </summary>
        /// <param name="musicID">MusicID</param>
        /// <param name="nowSec">今の時間(sec)</param>
        public void MusicTimePresent(int musicID, float nowSec)
        {
            //IDが一致したIndexに格納
            for (int i = 0; i < nowSecPresentData.Length; i++)
            {
                if(i != musicID) continue;
                nowSecPresentData[i] = nowSec;
                break;
            }
        }
    }

    /// <summary>
    /// NotePresenterのnowSecを更新するJob
    /// </summary>
    [BurstCompile]
    public partial struct NowSecPresentJob : IJobEntity
    {
        public NativeArray<float> nowSecPresentData;
        [BurstCompile]
        private void Execute(NotePresenterAspect notePresenterAspect)
        {
            for (int i = 0; i < nowSecPresentData.Length; i++)
            {
                if(i != notePresenterAspect.GetMusicID()) continue;
                notePresenterAspect.SetNowSec(nowSecPresentData[i]);
                break;
            }
        }
    }
}