using System;
using RhythmSystemEntities.Data;
using RhythmSystemEntities.System.Judge;
using UniRx;
using UnityEngine;

namespace RhythmSystemEntities.System.Player
{
    /// <summary>
    /// プレイヤークラス
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [SerializeField,Tooltip("プレイヤー入力クラスの参照")] private PlayerInputController playerInputController;
        [SerializeField,Tooltip("入力判定をECSに送るクラスの参照")] private InputJudgeSystemPresenter inputJudgePresenter;
        
        private void Start()
        {
            LaneInputSubscribe();
        }

        private void LaneInputSubscribe()
        {
            //入力の購読
            for (int i = 0; i < GameSettings.laneNum; i++)
            {
                int lane = i;
                
                playerInputController
                    .OnLaneInput(lane)
                    .Subscribe(value => LaneInput(lane,value))
                    .AddTo(this);
            }
        }

        //データを判定クラスに送る
        private void LaneInput(int laneNum,bool value)
        {
            inputJudgePresenter.LaneInput(laneNum,value);
        }

    }
}