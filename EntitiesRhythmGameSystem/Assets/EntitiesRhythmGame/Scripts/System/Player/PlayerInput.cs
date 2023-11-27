using System;
using RhythmSystemEntities.Data;
using UniRx;
using UnityEngine.InputSystem;

namespace RhythmSystemEntities.System.Player
{
    /// <summary>
    /// プレイヤーの入力クラス
    /// 主にレーン入力
    /// </summary>
    public class PlayerInput : IDisposable
    {
        //入力
        private MainGameActions mainGameActions;
        private InputAction[] laneInputActions;
        
        //購読用
        private Subject<bool>[] onLaneInputs;
        public IObservable<bool> OnLaneInputs(int laneNum) => onLaneInputs[laneNum];

        public PlayerInput()
        {
            mainGameActions = new MainGameActions();
            onLaneInputs = new Subject<bool>[GameSettings.laneNum];
            laneInputActions = new InputAction[GameSettings.laneNum];
            
            //入力の初期化
            InputInit();
        }

        private void InputInit()
        {
            //入力それぞれを配列に格納
            laneInputActions[0] = mainGameActions.Main.Lane0;
            laneInputActions[1] = mainGameActions.Main.Lane1;
            laneInputActions[2] = mainGameActions.Main.Lane2;
            laneInputActions[3] = mainGameActions.Main.Lane3;
            
            for (var i = 0; i < laneInputActions.Length; i++)
            {
                //有効化
                laneInputActions[i].Enable();
                
                //購読してsubjectイベントに変換
                onLaneInputs[i] = new Subject<bool>();
                int num = i;
                laneInputActions[i].performed += context =>
                {
                    onLaneInputs[num].OnNext(true);
                };
                laneInputActions[i].canceled += context =>
                {
                    onLaneInputs[num].OnNext(false);
                };
            }
        }

        public void Dispose()
        {
            //破棄
            mainGameActions?.Disable();
            mainGameActions?.Dispose();
            
            for (var i = 0; i < onLaneInputs.Length; i++)
            {
                onLaneInputs[i].Dispose();
            }
        }
    }
}