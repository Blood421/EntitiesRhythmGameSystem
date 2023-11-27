using RhythmSystemEntities.ECS.Data;
using Unity.Entities;

namespace RhythmSystemEntities.ECS.Aspect
{
    /// <summary>
    /// 曲ごとのオプション用のアスペクト
    /// </summary>
    public readonly partial struct NoteOptionAspect : IAspect
    {
        //ノーツのMusicID
        private readonly RefRO<MusicIDData> musicIDDataRO;
        //ノーツのオプションデータ
        private readonly RefRW<NoteOptionData> noteOptionDataRW;

        /// <summary>
        /// オプションのスクロールスピードの設定
        /// </summary>
        /// <param name="scrollSpeed">オプションのスクロールスピード</param>
        public void SetScrollSpeed(float scrollSpeed) => noteOptionDataRW.ValueRW.scrollSpeed = scrollSpeed;

        /// <summary>
        /// オプションのプレイヤーからの入力オフセットの設定
        /// </summary>
        /// <param name="offset">オプションのプレイヤーからの入力オフセット</param>
        public void SetPlayerInputOffset(float offset) => noteOptionDataRW.ValueRW.playerInputOffset = offset;
        
        /// <summary>
        /// MusicIDの取得
        /// </summary>
        /// <returns>MusicID</returns>
        public int GetMusicID() => musicIDDataRO.ValueRO.musicID;
    }
}