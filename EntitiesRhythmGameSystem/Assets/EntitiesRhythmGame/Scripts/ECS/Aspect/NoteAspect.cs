using RhythmSystemEntities.Data;
using RhythmSystemEntities.ECS.Data;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace RhythmSystemEntities.ECS.Aspect
{
    /// <summary>
    /// ノーツエンティティのアスペクト
    /// </summary>
    public readonly partial struct NoteAspect : IAspect
    {
        //エンティティ
        private readonly Entity entity;
        //LocalTransform
        private readonly RefRW<LocalTransform> localTransformRW;
        //LocalToWorld
        private readonly RefRW<LocalToWorld> localToWorldRW;
        //ノーツのMusicID
        private readonly RefRO<MusicIDData> musicIDDataRO;
        //ノーツのデータ
        private readonly RefRW<NoteComponentData> noteDataRW;
        //ノーツのオプションデータ
        private readonly RefRO<NoteOptionData> noteOptionDataRO;

        #region View
        
        /// <summary>
        /// 見た目の更新
        /// </summary>
        public void ViewUpdate()
        {
            //もし処理済みなら非表示にする
            bool isExecuted = noteDataRW.ValueRO.isExecuted;
            if (isExecuted)
            {
                SuspectedDisableToView();
                return;
            }
            
            //移動
            TransformUpdate();
        }
        
        /// <summary>
        /// ノーツのTransform更新
        /// </summary>
        private void TransformUpdate()
        {
            NoteComponentData.NoteType noteType = noteDataRW.ValueRO.noteType;
            if(noteType == NoteComponentData.NoteType.Single) SingleNoteTransformUpdate();
            else if(noteType == NoteComponentData.NoteType.Long) LongNoteTransformUpdate();
        }

        /// <summary>
        /// シングルノーツのTransform更新
        /// </summary>
        private void SingleNoteTransformUpdate()
        {
            //データを取得
            float beat = noteDataRW.ValueRO.nowBeat;
            float beginBeat = noteDataRW.ValueRO.timingData.beginBeat;
            float offsetBeat = noteDataRW.ValueRO.timingData.offsetBeat;
            int lane = noteDataRW.ValueRO.lane;
            float2 posOffset = noteDataRW.ValueRO.posOffset;
            float2 posAdjustor = noteDataRW.ValueRO.posAdjustor;

            //スクロール速度
            float scrollSpeed = noteOptionDataRO.ValueRO.scrollSpeed;
            
            //移動
            SingleNoteMove(new float3(lane * posAdjustor.x + posOffset.x,(beginBeat - beat + offsetBeat) * posAdjustor.y * scrollSpeed + posOffset.y , 0));
        }
        
        /// <summary>
        /// シングルノーツの移動
        /// </summary>
        /// <param name="pos">更新位置</param>
        private void SingleNoteMove(float3 pos)
        {
            //移動
            localTransformRW.ValueRW.Position = pos;
        }
        
        /// <summary>
        /// ロングノーツのTransform更新
        /// </summary>
        private void LongNoteTransformUpdate()
        {
            //データを取得
            float nowBeat = noteDataRW.ValueRO.nowBeat;
            float beginBeat = noteDataRW.ValueRO.timingData.beginBeat;
            float endBeat = noteDataRW.ValueRO.timingData.endBeat;
            float midBeat = (endBeat + beginBeat) / 2f;
            float offsetBeat = noteDataRW.ValueRO.timingData.offsetBeat;
            int lane = noteDataRW.ValueRO.lane;
            float2 posOffset = noteDataRW.ValueRO.posOffset;
            float2 posAdjustor = noteDataRW.ValueRO.posAdjustor;

            //スクロール速度
            float scrollSpeed = noteOptionDataRO.ValueRO.scrollSpeed;

            //移動
            LongNoteMove(new float3(lane * posAdjustor.x + posOffset.x,(midBeat - nowBeat + offsetBeat) * posAdjustor.y * scrollSpeed + posOffset.y , 0));
            //スケール変化
            LongNoteScaleUpdate(endBeat - beginBeat);
        }

        /// <summary>
        /// ロングノーツの移動
        /// </summary>
        /// <param name="pos">更新位置</param>
        private void LongNoteMove(float3 pos)
        {
            //移動
            localTransformRW.ValueRW.Position = pos;
        }

        /// <summary>
        /// ロングノーツの大きさの変更
        /// </summary>
        /// <param name="beatDif">終わりと始まりのbeatの差</param>
        private void LongNoteScaleUpdate(float beatDif)
        {
            var l2w = localToWorldRW.ValueRW.Value;
            
            //スクロール速度
            float scrollSpeed = noteOptionDataRO.ValueRO.scrollSpeed;
            
            l2w.c1.y = beatDif * scrollSpeed;
            localToWorldRW.ValueRW.Value = l2w;
        }
        
        /// <summary>
        /// 疑似的に非表示にする
        /// 描画範囲外に吹っ飛ばすことで非表示に
        /// </summary>
        private void SuspectedDisableToView() => localTransformRW.ValueRW.Position = new float3(-10000,-10000,-10000);


        #endregion

        /// <summary>
        /// 今の時間をセット(sec)
        /// </summary>
        /// <param name="sec">今のsec</param>
        public void SetNowSec(float sec) => noteDataRW.ValueRW.nowSec = sec;
        
        /// <summary>
        /// 今の時間をセット(beat)
        /// </summary>
        /// <param name="beat">今のbeat</param>
        public void SetNowBeat(float beat) => noteDataRW.ValueRW.nowBeat = beat;

        /// <summary>
        /// ノーツを判定処理する
        /// </summary>
        public void ExecuteNote(GameSettings.JudgeType judgeType)
        {
            var type = noteDataRW.ValueRO.noteType;
            //ロングノーツかつ処理中じゃなくミス判定じゃない場合処理中に変更
            if (type == NoteComponentData.NoteType.Long && !GetIsProcessing() && judgeType != GameSettings.JudgeType.Miss)
            {
                noteDataRW.ValueRW.isProcessing = true;
                return;
            }
            noteDataRW.ValueRW.isExecuted = true;
        }

        /// <summary>
        /// ノーツを復活させることがあれば
        /// </summary>
        public void ReviveNote()
        {
            noteDataRW.ValueRW.isExecuted = false;
            noteDataRW.ValueRW.isProcessing = false;
        }

        
        /// <summary>
        /// musicIDの取得
        /// </summary>
        /// <returns>ノーツのMusicID</returns>
        public int GetMusicID() => musicIDDataRO.ValueRO.musicID;

        /// <summary>
        /// LongNoteの端のIDの取得
        /// </summary>
        /// <returns>LongNoteの端のID</returns>
        public NoteComponentData.LongNoteEdgeIDData GetLongNoteEdgeIDData() => noteDataRW.ValueRO.longNoteEdgeIDData;

        /// <summary>
        /// Laneの取得
        /// </summary>
        /// <returns>ノーツのLane</returns>
        public int GetLane() => noteDataRW.ValueRO.lane;

        /// <summary>
        /// Noteの種類の取得
        /// </summary>
        /// <returns>NoteType</returns>
        public NoteComponentData.NoteType GetNoteType() => noteDataRW.ValueRO.noteType;

        /// <summary>
        /// ノーツが処理中かどうかの取得
        /// </summary>
        /// <returns>処理中はtrue</returns>
        public bool GetIsProcessing() => noteDataRW.ValueRO.isProcessing;

        /// <summary>
        /// ノーツが処理済みかどうかの取得
        /// </summary>
        /// <returns>処理済みはtrue</returns>
        public bool GetIsExecuted() => noteDataRW.ValueRO.isExecuted;

        /// <summary>
        /// EntityのIndex
        /// </summary>
        /// <returns>EntityのIndexを返す</returns>
        public int GetEntityIndex() => entity.Index;

        /// <summary>
        /// Noteの判定Sec
        /// </summary>
        /// <returns>Noteの判定Secを返す</returns>
        public float GetJudgeSec()
        {
            var type = noteDataRW.ValueRO.noteType;
            //シングル
            if (type == NoteComponentData.NoteType.Single) return noteDataRW.ValueRO.timingData.beginSec;
            else
            {
                //ロング
                bool isProcessing = noteDataRW.ValueRO.isProcessing;
                //終点
                if (isProcessing)return noteDataRW.ValueRO.timingData.endSec;
                
                //始点
                return noteDataRW.ValueRO.timingData.beginSec;
            }
        }

        /// <summary>
        /// オフセットを返す
        /// NowSecから引いて使ってね
        /// </summary>
        /// <returns>オフセット</returns>
        public float GetJudgeOffsetSec() => noteDataRW.ValueRO.timingData.offsetSec;

        /// <summary>
        /// オプションの入力オフセットを返す
        /// NowSecに足して使ってね
        /// </summary>
        /// <returns>オプションの入力オフセット</returns>
        public float GetPlayerInputOffsetSec() => noteOptionDataRO.ValueRO.playerInputOffset;

        /// <summary>
        /// 判定する範囲に入っているかどうかのチェック
        /// </summary>
        /// <returns>範囲内はTrue,範囲外はFalse</returns>
        public bool WithinJudgeRangeCheck()
        {
            //処理済みは常にfalseを返す
            if (noteDataRW.ValueRO.isExecuted) return false;
            
            var type = noteDataRW.ValueRO.noteType;
            float nowSec = noteDataRW.ValueRO.nowSec - GetJudgeOffsetSec() + GetPlayerInputOffsetSec();
            float beginSec = noteDataRW.ValueRO.timingData.beginSec;
            float endSec = noteDataRW.ValueRO.timingData.endSec;
            float judgeSec = (beginSec - nowSec);
            float missRange = GameSettings.missRange;
            
            //判定
            //シングル
            if (type == NoteComponentData.NoteType.Single) return Mathf.Abs(judgeSec) <= missRange;
            else
            {
                //ロング
                bool isProcessing = noteDataRW.ValueRO.isProcessing;
                //終点
                if (isProcessing)
                {
                    judgeSec = (endSec - nowSec);
                    //負なら範囲内ならtrue
                    //正なら常にtrue
                    //ロングノーツなので離れる時にしか終点判定はないはず
                    if(judgeSec < 0) return -missRange <= judgeSec;
                     return true;
                }
                //始点
                return Mathf.Abs(judgeSec) <= missRange;
            }
        }

        /// <summary>
        /// 判定通り越したノーツの処理
        /// </summary>
        /// <param name="isAuto">オート処理かどうか</param>
        /// <returns>処理した場合はTrue</returns>
        public bool ThroughNote(bool isAuto)
        {
            //処理済みならスキップ
            if (noteDataRW.ValueRO.isExecuted) return false;
            
            float nowSec = noteDataRW.ValueRO.nowSec - GetJudgeOffsetSec() + GetPlayerInputOffsetSec();
            float judgeSec = noteDataRW.ValueRO.timingData.beginSec;
            //処理中なら終わりのsecを入れる
            if (noteDataRW.ValueRO.isProcessing) judgeSec = noteDataRW.ValueRO.timingData.endSec;
            
            //判定に使う差分
            //オートなら0
            float judgeSecDiff = GameSettings.missRange;
            if (isAuto) judgeSecDiff = 0;

            //処理判定
            if (nowSec - judgeSec > judgeSecDiff)
            {
                //オートならパーフェクトで処理
                //オートじゃなければミスで処理
                GameSettings.JudgeType judgeType = GameSettings.JudgeType.Miss;
                if (isAuto) judgeType = GameSettings.JudgeType.Perfect;
                ExecuteNote(judgeType);
                return true;
            }

            //処理しなかった
            return false;
        }

        /// <summary>
        /// 判定されたポジションを取得
        /// </summary>
        /// <returns>判定されたポジション</returns>
        public float3 GetNoteJudgedPos()
        {
            //返す物
            float3 pos;
            
            //データを取得
            float nowBeat = noteDataRW.ValueRO.nowBeat;
            float beginBeat = noteDataRW.ValueRO.timingData.beginBeat;
            float endBeat = noteDataRW.ValueRO.timingData.endBeat;

            float offsetBeat = noteDataRW.ValueRO.timingData.offsetBeat;
            int lane = noteDataRW.ValueRO.lane;
            float2 posOffset = noteDataRW.ValueRO.posOffset;
            float2 posAdjustor = noteDataRW.ValueRO.posAdjustor;

            //スクロール速度
            float scrollSpeed = noteOptionDataRO.ValueRO.scrollSpeed;
            
            NoteComponentData.NoteType noteType = GetNoteType();
            if (noteType == NoteComponentData.NoteType.Long)
            {
                //ビート
                //始点or終点
                float beat = beginBeat;
                if (GetIsExecuted()) beat = endBeat;

                pos = new float3(lane * posAdjustor.x + posOffset.x,
                    (beat - nowBeat + offsetBeat) * posAdjustor.y * scrollSpeed + posOffset.y,
                    0);
                return pos;
            }

            pos = (new float3(lane * posAdjustor.x + posOffset.x,
                (beginBeat - nowBeat + offsetBeat) * posAdjustor.y * scrollSpeed + posOffset.y,
                0));
            return pos;
        }
    }
}