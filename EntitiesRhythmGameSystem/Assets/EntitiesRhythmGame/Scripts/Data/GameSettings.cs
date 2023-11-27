namespace RhythmSystemEntities.Data
{
    /// <summary>
    /// ゲームの設定
    /// 基本的にconstの値のみ
    /// </summary>
    public readonly struct GameSettings
    {
        //判定の種類
        public enum JudgeType
        {
            Miss = 0,
            Good = 1,
            Perfect = 2,
        }

        //最大同時曲数
        public const int musicNumMax = 1;
        //レーン最大数
        public const int laneNum = 4;
        //判定の許容範囲
        public const float missRange = 0.4f;
        public const float goodRange = 0.3f;
        public const float perfectRange = 0.2f;
    }
}