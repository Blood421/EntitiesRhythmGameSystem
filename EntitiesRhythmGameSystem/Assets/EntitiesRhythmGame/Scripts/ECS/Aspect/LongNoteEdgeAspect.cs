using RhythmSystemEntities.ECS.Data;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace RhythmSystemEntities.ECS.Aspect
{
    /// <summary>
    /// ロングノーツの端のエンティティのアスペクト
    /// </summary>
    public readonly partial struct LongNoteEdgeAspect : IAspect
    {
        //エンティティ
        private readonly Entity entity;
        //LocalTransform
        private readonly RefRW<LocalTransform> localTransformRW;
        //ノーツのMusicID
        private readonly RefRO<MusicIDData> musicIDDataRO;
        //ノーツのデータ
        private readonly RefRW<LongNoteEdgeData> longNoteEdgeDataRW;
        //ノーツのオプションデータ
        private readonly RefRO<NoteOptionData> noteOptionDataRO;

        #region View

        /// <summary>
        /// 見た目の更新
        /// </summary>
        public void ViewUpdate()
        {
            //処理済みなら描画しない
            if (longNoteEdgeDataRW.ValueRO.isExecuted)
            {
                SuspectedDisableToView();
                return;
            }
            //移動
            Move();
        }
        
        /// <summary>
        /// 移動
        /// </summary>
        private void Move()
        {
            //データを取得
            float nowBeat = longNoteEdgeDataRW.ValueRO.nowBeat;
            float beat = longNoteEdgeDataRW.ValueRO.timingData.beat;
            float offsetBeat = longNoteEdgeDataRW.ValueRO.timingData.offsetBeat;
            int lane = longNoteEdgeDataRW.ValueRO.lane;
            float2 posOffset = longNoteEdgeDataRW.ValueRO.posOffset;
            float2 posAdjustor = longNoteEdgeDataRW.ValueRO.posAdjustor;
            
            //スクロール速度
            float scrollSpeed = noteOptionDataRO.ValueRO.scrollSpeed;
            
            //移動
            //zをほんの少し手前に
            localTransformRW.ValueRW.Position = new float3(lane * posAdjustor.x + posOffset.x,(beat - nowBeat + offsetBeat) * posAdjustor.y * scrollSpeed + posOffset.y , -0.01f);
        }
        
        /// <summary>
        /// 疑似的に非表示にする
        /// 描画範囲外に吹っ飛ばすことで非表示に
        /// </summary>
        private void SuspectedDisableToView() => localTransformRW.ValueRW.Position = new float3(-10000,-10000,-10000);

        #endregion
        
        /// <summary>
        /// 今の時間をセット(beat)
        /// </summary>
        /// <param name="beat">今のbeat</param>
        public void SetNowBeat(float beat) => longNoteEdgeDataRW.ValueRW.nowBeat = beat;

        /// <summary>
        /// 処理済みかどうかの設定
        /// </summary>
        /// <param name="value">処理済みならtrue</param>
        public void SetIsExecuted(bool value) => longNoteEdgeDataRW.ValueRW.isExecuted = value;

        /// <summary>
        /// musicIDの取得
        /// </summary>
        /// <returns>ノーツのMusicID</returns>
        public int GetMusicID() => musicIDDataRO.ValueRO.musicID;

        /// <summary>
        /// edgeIDの取得
        /// </summary>
        /// <returns>edgeID</returns>
        public int GetEdgeID() => longNoteEdgeDataRW.ValueRO.edgeID;
    }
}