using RhythmSystemEntities.ECS.Data;

namespace RhythmSystemEntities.Data
{
    /// <summary>
    /// 譜面データをエンティティにするまでの仮データ
    /// </summary>
    public struct NotePropertyTemp
    {
        //ノーツの種類
        public NoteComponentData.NoteType noteType;
        //ノーツの始まるbeatと終わるbeat
        public float beginBeat;
        public float endBeat;
        
        //レーン
        public int lane;    
    }
}