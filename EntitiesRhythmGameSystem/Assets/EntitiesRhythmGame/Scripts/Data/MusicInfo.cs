namespace RhythmSystemEntities.Data
{
    /// <summary>
    /// 譜面データから読み込んだ曲情報
    /// </summary>
    public class MusicInfo
    {
        public readonly string genre;
        public readonly string title;
        public readonly string subTitle;
        public readonly string artist;
        public readonly string subArtist;
        public readonly string notesArtist;
        public readonly float bpm;
        public readonly int rank;
        public readonly int level;
        public readonly float offset;
        public readonly string comment;
        
        public MusicInfo(
            string genre,
            string title,
            string subTitle,
            string artist,
            string subArtist,
            string notesArtist,
            float bpm,
            int rank,
            int level,
            float offset,
            string comment
        )
        {
            this.genre = genre;
            this.title = title;
            this.subTitle = subTitle;
            this.artist = artist;
            this.subArtist = subArtist;
            this.notesArtist = notesArtist;
            this.bpm = bpm;
            this.rank = rank;
            this.level = level;
            this.offset = offset;
            this.comment = comment;
        }
    }
}