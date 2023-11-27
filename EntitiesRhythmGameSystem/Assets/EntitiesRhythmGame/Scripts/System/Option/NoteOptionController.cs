using System;
using UniRx;
using UnityEngine;

namespace RhythmSystemEntities.System.Option
{
    /// <summary>
    /// ノーツのオプションデータクラス
    /// </summary>
    public class NoteOptionController : MonoBehaviour
    {
        [SerializeField,Tooltip("スクロール速度")] private FloatReactiveProperty scrollSpeed;
        public IReadOnlyReactiveProperty<float> GetScrollSpeed() => scrollSpeed;
        [SerializeField,Tooltip("プレイヤーの入力オフセット")] private FloatReactiveProperty playerInputOffset;
        public IReadOnlyReactiveProperty<float> GetPlayerInputOffset() => playerInputOffset;

        private void OnDestroy()
        {
            scrollSpeed.Dispose();
        }
    }
}