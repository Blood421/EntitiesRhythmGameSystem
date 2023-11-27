using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using RhythmSystemEntities.Data;
using UnityEngine;

namespace RhythmSystemEntities.System.Loader
{
    /// <summary>
    /// 譜面をロードするクラス
    /// </summary>
    public class NotesGenerateData
    {
        //譜面データ
        private List<NotePropertyTemp> noteProperties = new List<NotePropertyTemp>(10000);
        //BPM変化データ
        private List<BPMChange> bpmChanges = new List<BPMChange>(10000);
        //曲情報
        private MusicInfo musicInfo;
        //譜面のパス
        private string dataFilePath = "";

        
        public NotesGenerateData(string dataFilePath)
        {
            this.dataFilePath = dataFilePath;
        }

        /// <summary>
        /// 譜面のロード
        /// </summary>
        public void Load()
        {
            if (this.dataFilePath.Contains(".djbm"))
            {
                DJBMLoad();
                Debug.Log("DJBMロード");
                return;
            }
            Debug.LogError("拡張子が不正です");
        }

        /// <summary>
        /// 譜面の非同期ロード
        /// </summary>
        /// <param name="token">キャンセレーショントークン</param>
        public async UniTask LoadAsync(CancellationToken token)
        {
            if (this.dataFilePath.Contains(".djbm"))
            {
                await DJBMLoadAsync(token);
                Debug.Log("DJBMロード");
                return;
            }
            Debug.LogError("拡張子が不正です");
        }

        //djbmファイルのロード
        private void DJBMLoad()
        {
            NotesGenerateDataLoaderDJBM loader = new NotesGenerateDataLoaderDJBM(dataFilePath);

            loader.Load();
            noteProperties = loader.GetNoteProperties();
            bpmChanges = loader.GetBPMChanges();
            musicInfo = loader.GetMusicInfo();
        }

        //djbmファイルのロード
        private async UniTask DJBMLoadAsync(CancellationToken token)
        {
            NotesGenerateDataLoaderDJBM loader = new NotesGenerateDataLoaderDJBM(dataFilePath);
            //100行読むのに1フレーム設定
            await loader.LoadAsync(100,token);
            
            noteProperties = loader.GetNoteProperties();
            bpmChanges = loader.GetBPMChanges();
            musicInfo = loader.GetMusicInfo();
        }
        
        //各種ロードデータ取得用
        public List<NotePropertyTemp> GetNoteProperties() => noteProperties;
        public List<BPMChange> GetBPMChanges() => bpmChanges;
        public MusicInfo GetMusicInfo() => musicInfo;
    }
}