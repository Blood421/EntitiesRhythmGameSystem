using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using RhythmSystemEntities.ECS.System;
using RhythmSystemEntities.System.Judge;
using TMPro;
using UnityEngine;

namespace RhythmSystemEntities.System.UI
{
    /// <summary>
    /// テキストに判定結果を表示するクラス
    /// </summary>
    public class JudgeResultText : JudgeReceiverItemBase
    {
        [SerializeField,Tooltip("リザルトを表示するテキスト")] private TextMeshProUGUI resultText;
        //判定が入るキュー
        private Queue<NoteJudgeSystem.JudgeNotifyData> judgeNotifyDataQueue;
        //ディレイデキュー用
        private float time = 0;

        private void Awake()
        {
            //初期化
            judgeNotifyDataQueue = new Queue<NoteJudgeSystem.JudgeNotifyData>();
            time = 0;
        }

        private void Update()
        {
            //時間更新
            time += Time.deltaTime;
            if (time > 0.05f)
            {
                //1秒待ってデキューしてテキストを更新
                Dequeue();
                time = 0;
            }
        }

        //判定データが送られてくる
        public override void Notified(NoteJudgeSystem.JudgeNotifyData data)
        {
            EnqueueResult(data);
        }

        //リザルトをエンキューして表示
        private void EnqueueResult(NoteJudgeSystem.JudgeNotifyData data)
        {
            judgeNotifyDataQueue.Enqueue(data);
            UpdateText();
        }

        //テキストを更新
        private void UpdateText()
        {
            resultText.text = string.Empty;

            int count = 0;
            foreach (NoteJudgeSystem.JudgeNotifyData judgeNotifyData in judgeNotifyDataQueue)
            {
                //描画しすぎるとスパイクとんでもないことになるから制限
                if (count >= 10) return;
                resultText.text += "MusicID : <color=blue>"
                                   + judgeNotifyData.musicID
                                   + "</color> , Lane : <color=orange>"
                                   + judgeNotifyData.lane
                                   + "</color> , IsExecuted : <color=red>"
                                   + judgeNotifyData.isExecuted
                                   + "\n"
                                   + "</color> , pos : <color=green>"
                                   + " x:"
                                   + judgeNotifyData.noteJudgedPos.x
                                   + " y:"
                                   + judgeNotifyData.noteJudgedPos.y
                                   + " z:"
                                   + judgeNotifyData.noteJudgedPos.z
                                   + "</color> , Judge : <color=yellow>"
                                   + judgeNotifyData.judgeType
                                   + "</color>";
                resultText.text += "\n";
                count++;
            }
        }

        //デキューしてテキスト更新
        private void Dequeue()
        {
            if(judgeNotifyDataQueue.Count == 0) return;
            judgeNotifyDataQueue.Dequeue();
            UpdateText();
        }
    }
}