using RhythmSystemEntities.ECS.System;
using UniRx;
using Unity.Entities;
using UnityEngine;

namespace RhythmSystemEntities.System.Judge
{
    /// <summary>
    /// 判定データをECS側から受け取ってReceiverに流すクラス
    /// </summary>
    public class JudgeRecieverFromECS : MonoBehaviour
    {
        //通知を流したいクラス
        [SerializeField] private JudgeReceiverItemBase[] judgeReceiverItemBases;
        private void Start()
        {
            //判定システムを取得
            NoteJudgeSystem noteJudgeSystem =
                World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<NoteJudgeSystem>();

            //判定システムから判定通知が来たらデータを流す
            noteJudgeSystem
                .OnJudge()
                .Subscribe(result =>
                {
                    foreach (JudgeReceiverItemBase judgeReceiverItemBase in judgeReceiverItemBases)
                    {
                        //通知
                        judgeReceiverItemBase.Notified(result);
                    }
                })
                .AddTo(this);
        }
    }
}