namespace RhythmSystemEntities.Data
{
    /// <summary>
    /// BPM変化に関するデータ
    /// </summary>
    public struct BPMChange
    {
        //BPM変化を実行するBeat
        public float executeBeat;
        //変化後のBPM
        public float afterBPM;
    }
}