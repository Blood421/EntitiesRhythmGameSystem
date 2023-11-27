using Unity.Entities;
using Unity.Mathematics;

namespace RhythmSystemEntities.ECS.Data
{
    public struct NoteComponentData : IComponentData
    {
        //タイミング系のデータ
        public struct TimingData
        {
            //始まる時間(sec)
            public float beginSec;
            //終わる時間(sec)
            public float endSec;
            //始まる時間(beat)
            public float beginBeat;
            //終わる時間(beat)
            public float endBeat;
            //曲とのズレを修正するもの(sec)
            public float offsetSec;
            //曲とのズレを修正するもの(beat)
            public float offsetBeat;
        }
        
        //ロングノーツの端のIDデータ
        public struct LongNoteEdgeIDData
        {
            public int beginID;
            public int endID;
        }
        
        //ノーツの種類
        public enum NoteType
        {
            Single,
            Long,
        }

        //ノーツの種類
        public NoteType noteType;
        //タイミング系のデータ
        public TimingData timingData;
        //ロングノーツの端のIDデータ
        public LongNoteEdgeIDData longNoteEdgeIDData;
        
        //今の時間(sec)
        public float nowSec;
        //今の時間(beat)
        public float nowBeat;
        
        //処理中かどうか(ロング)
        public bool isProcessing;
        //処理済みノーツかどうか
        public bool isExecuted;

        //レーン
        public int lane;
        //ポジションのオフセット
        public float2 posOffset;
        //ポジションの調整(間隔)
        public float2 posAdjustor;
    }
}
