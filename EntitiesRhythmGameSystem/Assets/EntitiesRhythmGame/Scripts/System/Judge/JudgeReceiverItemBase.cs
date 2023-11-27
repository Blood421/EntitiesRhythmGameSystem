using RhythmSystemEntities.ECS.System;
using UnityEngine;

namespace RhythmSystemEntities.System.Judge
{
    /// <summary>
    /// 判定を受け取るクラスの基底クラス
    /// </summary>
    public abstract class JudgeReceiverItemBase : MonoBehaviour
    {
        /// <summary>
        /// 判定通知を受けとる
        /// </summary>
        /// <param name="data">判定データ</param>
        public abstract void Notified(NoteJudgeSystem.JudgeNotifyData data);
    }
}