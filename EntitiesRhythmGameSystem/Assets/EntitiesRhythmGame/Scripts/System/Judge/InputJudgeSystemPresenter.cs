using System;
using RhythmSystemEntities.Data;
using RhythmSystemEntities.ECS.System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace RhythmSystemEntities.System.Judge
{
    /// <summary>
    /// 判定データをECS側に流すクラス
    /// </summary>
    public class InputJudgeSystemPresenter : MonoBehaviour
    {
        //入力データ
        public class LaneInputData
        {
            public bool input;
            public bool isThisFrameInput;

            public LaneInputData()
            {
                input = false;
                isThisFrameInput = false;
            }
        }

        [SerializeField,Tooltip("曲ID")] private int musicID = 0;
        [SerializeField,Tooltip("オートにするかどうか")] private bool isAuto;
        private LaneInputData[] laneInputDataArray;
        private NoteJudgeSystem noteJudgeSystem;
        
        private void Awake()
        {
            //配列初期化
            //レーン == 配列のindex
            laneInputDataArray = new LaneInputData[GameSettings.laneNum];
            for (int i = 0; i < GameSettings.laneNum; i++)
            {
                int lane = i;
                laneInputDataArray[lane] = new LaneInputData();
            }
        }

        private void Start()
        {
            //システムを取得
            noteJudgeSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<NoteJudgeSystem>();
            //システムに初期値を送る
            noteJudgeSystem.AddMusicSettings(new NoteJudgeSystem.JudgeMusicSettings()
            {
                musicID = musicID,
                isAuto = isAuto,
            });
        }

        /// <summary>
        /// レーンの入力
        /// </summary>
        /// <param name="laneNum">レーン</param>
        /// <param name="value">押したtrue,話したfalse</param>
        public void LaneInput(int laneNum,bool value)
        {
            laneInputDataArray[laneNum].input = value;
            laneInputDataArray[laneNum].isThisFrameInput = true;
        }
        
                
        private void Update()
        {
            SendInputDataToECS();
        }

        /// <summary>
        /// データに変更があったら(入力があったら)ECS側に送る
        /// </summary>
        private void SendInputDataToECS()
        {
            //LateUpdateで呼ぶ

            //データを送るかどうか
            bool isSendDataToECS = false;
            for (int i = 0; i < GameSettings.laneNum; i++)
            {
                //このフレームで入力がなければ(使用済みなら)処理しない
                if (!laneInputDataArray[i].isThisFrameInput) continue;
                isSendDataToECS = true;
                break;
            }
            //送らないならスキップ
            if (!isSendDataToECS) return;
            
            //ECSで使うデータを作成
            var judgedInputData = new NativeArray<NoteJudgeSystem.JudgedInputData>(GameSettings.laneNum,Allocator.TempJob);
            for (int lane = 0; lane < judgedInputData.Length; lane++)
            {
                //データをセット
                judgedInputData[lane] = new NoteJudgeSystem.JudgedInputData()
                {
                    musicID = musicID,
                    isJudge = laneInputDataArray[lane].isThisFrameInput,
                    isPressed = laneInputDataArray[lane].input,
                };

                //データを使用済みに戻す
                laneInputDataArray[lane].isThisFrameInput = false;
                //ECS側に入力を送る
                //Debug.Log(lane + " : " + laneInputDataArray[lane].input);
            }
            
            //ECSの判定処理
            noteJudgeSystem.Judge(judgedInputData);
        }
    }
}