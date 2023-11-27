using Unity.Entities;
using Unity.Mathematics;

namespace RhythmSystemEntities.ECS.Data
{
    /// <summary>
    /// ロングノーツの端のエンティティのデータ
    /// </summary>
    public struct LongNoteEdgeData : IComponentData
    {
        //タイミング系のデータ
        public struct TimingData
        {
            //時間(beat)
            public float beat;
            //曲とのズレを修正するもの(beat)
            public float offsetBeat;
        }

        //EdgeID
        public int edgeID;
        
        //タイミング系のデータ
        public TimingData timingData;

        //今の時間(beat)
        public float nowBeat;

        //レーン
        public int lane;
        
        //処理したかどうか
        public bool isExecuted;
        //ポジションのオフセット
        public float2 posOffset;
        //ポジションの調整(間隔)
        public float2 posAdjustor;
    }
}