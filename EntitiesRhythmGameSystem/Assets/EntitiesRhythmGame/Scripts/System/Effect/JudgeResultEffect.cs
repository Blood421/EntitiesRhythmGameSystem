using RhythmSystemEntities.Data;
using RhythmSystemEntities.ECS.System;
using RhythmSystemEntities.System.Judge;
using UnityEngine;

namespace RhythmSystemEntities.System.Effect
{
    /// <summary>
    /// 判定結果でエフェクトを表示するクラス
    /// </summary>
    public class JudgeResultEffect : JudgeReceiverItemBase
    {
        [SerializeField, Tooltip("曲ID")] private int musicID;
        [SerializeField, Tooltip("判定")] private GameSettings.JudgeType judgeType;
        [SerializeField, Tooltip("レーン")] private int lane;
        [SerializeField, Tooltip("表示するパーティクルシステム")]
        private ParticleSystem resultParticle;
        
        public override void Notified(NoteJudgeSystem.JudgeNotifyData data)
        {
            //曲IDと判定とレーンが一致したらプレイ
            if (data.judgeType != judgeType || data.lane != lane || musicID != data.musicID) return;
            //位置をノーツを判定した位置に
            Vector3 pos = data.noteJudgedPos;
            resultParticle.transform.position = pos;
            resultParticle.Play();
        }
    }
}