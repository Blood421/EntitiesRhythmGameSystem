using RhythmSystemEntities.ECS.Data;
using Unity.Entities;

namespace RhythmSystemEntities.ECS.Aspect
{
    /// <summary>
    /// ノーツへデータを運ぶ(Presenter)エンティティ用のアスペクト
    /// </summary>
    public readonly partial struct NotePresenterAspect : IAspect
    {
        //エンティティ
        private readonly Entity entity;
        //ノーツのMusicID
        private readonly RefRO<MusicIDData> musicIDDataRO;
        //NotePresenterのData
        private readonly RefRW<NotePresenterData> notePresenterDataRW;
        //ノーツのオプションデータ
        private readonly RefRW<NoteOptionData> noteOptionDataRW;
        
        /// <summary>
        /// 今の時間のセット(sec)
        /// </summary>
        /// <param name="value">今の時間(sec)</param>
        public void SetNowSec(float value) => notePresenterDataRW.ValueRW.nowSec = value;
        
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
        /// 今の時間の取得(sec)
        /// </summary>
        /// <returns>今の時間(sec)</returns>
        public float GetNowSec() => notePresenterDataRW.ValueRO.nowSec;
        
        /// <summary>
        /// MusicIDの取得
        /// </summary>
        /// <returns>MusicID</returns>
        public int GetMusicID() => musicIDDataRO.ValueRO.musicID;

        /// <summary>
        /// オプションのスクロール速度の取得
        /// </summary>
        /// <returns>オプションのスクロール速度</returns>
        public NoteOptionData GetOptionData() => noteOptionDataRW.ValueRO;
        
        /// <summary>
        /// BPMChangesのBlobAssetのRefを取得
        /// </summary>
        /// <returns>BPMChangesのBlobAssetのRef</returns>
        public BlobAssetReference<BPMChangesBlob> GetBPMChangesBlob() => notePresenterDataRW.ValueRO.blobAssetReference;

        /// <summary>
        /// Disposeする
        /// </summary>
        public void Dispose() => notePresenterDataRW.ValueRO.blobAssetReference.Dispose();
    }
}