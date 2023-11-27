using System;
using RhythmSystemEntities.Data;
using UniRx;
using UnityEngine;

namespace RhythmSystemEntities.System.Player
{
    /// <summary>
    /// プレイヤーの入力をまとめるクラス
    /// </summary>
    public class PlayerInputController : MonoBehaviour
    {
        //InputSystemからの入力
        private PlayerInput playerInput;
        //TODO:他デバイスからの入力があればここで連結

        //レーン入力用Subject
        private Subject<bool>[] onLaneInput;
        public IObservable<bool> OnLaneInput(int laneNum) => onLaneInput[laneNum];

        private void Awake()
        {
            //初期化
            playerInput = new PlayerInput();
            onLaneInput = new Subject<bool>[GameSettings.laneNum];
            for (int i = 0; i < GameSettings.laneNum; i++) onLaneInput[i] = new Subject<bool>();
        }

        private void Start()
        {
            InputSubscribe();
        }
        
        private void InputSubscribe()
        {
            //入力を購読して本クラスのSubjectに繋ぐ
            for (int i = 0; i < GameSettings.laneNum; i++)
            {
                int lane = i;
                playerInput
                    .OnLaneInputs(lane)
                    .Subscribe(value =>
                    {
                        onLaneInput[lane].OnNext(value);
                    })
                    .AddTo(this);
            }
        }
        
        private void OnDestroy()
        {
            //破棄
            playerInput.Dispose();

            for (int i = 0; i < GameSettings.laneNum; i++)
            {
                onLaneInput[i]?.Dispose();
            }
        }
    }
}