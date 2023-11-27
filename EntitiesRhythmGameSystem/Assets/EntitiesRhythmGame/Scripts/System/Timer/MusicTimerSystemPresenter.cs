using System;
using RhythmSystemEntities.ECS.System;
using UniRx;
using Unity.Entities;
using UnityEngine;

namespace RhythmSystemEntities.System.Timer
{
    /// <summary>
    /// 曲タイマーからECSに時間を送るクラス
    /// </summary>
    public class MusicTimerSystemPresenter : MonoBehaviour
    {
        [SerializeField,Tooltip("曲タイマーの参照")] private MusicTimer musicTimer;
        [SerializeField,Tooltip("曲ID")] private int musicID;

        private void Start()
        {
            //システムを取得
            TimePresenterSystem timePresenterSystem =
                World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<TimePresenterSystem>();

            //タイマーから時間をSystemに送るための購読
            musicTimer
                .GetTime()
                .Subscribe(nowSec =>
                {
                    timePresenterSystem.MusicTimePresent(musicID,nowSec);
                })
                .AddTo(this);
        }
    }
}