using System;
using Cysharp.Threading.Tasks;
using RhythmSystemEntities.Data;
using RhythmSystemEntities.ECS.Data;
using RhythmSystemEntities.ECS.System;
using RhythmSystemEntities.System.Loader;
using RhythmSystemEntities.System.Option;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace RhythmSystemEntities.System
{
    /// <summary>
    /// ノーツを生成するクラス
    /// </summary>
    public class NotesGenerator : MonoBehaviour
    {
        [SerializeField,Tooltip("曲ID")] private int musicID;
        [SerializeField,Tooltip("DJBMファイルのパス")] private string djbmPath;
        [SerializeField,Tooltip("オプションの参照")] private NoteOptionController noteOptionController;

        private void Start()
        {
            //非同期ロード
            LoadAsyncStart().Forget();
        }

        private async UniTask LoadAsyncStart()
        {
            //1秒後にロード
            await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: destroyCancellationToken);
            LoadAsync().Forget();
        }

        private async UniTask LoadAsync()
        {
            //譜面をテキストデータからロード
            NotesGenerateData notesGenerateData = new NotesGenerateData(djbmPath);
            await notesGenerateData.LoadAsync(destroyCancellationToken);
            
            //ロードデータを取得
            NotePropertyTemp[] notePropertyTemps = notesGenerateData.GetNoteProperties().ToArray();
            BPMChange[] bpmChanges = notesGenerateData.GetBPMChanges().ToArray();
            MusicInfo info = notesGenerateData.GetMusicInfo();

            //ノーツ生成システムを取得
            NoteAllDataSpawnSystem noteAllDataSpawnSystem =
                World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<NoteAllDataSpawnSystem>();

            //ノーツをスポーンさせる
            noteAllDataSpawnSystem.Spawn(new NoteAllDataSpawnSystem.GenerateData()
            {
                isUsed = false,
                musicID = musicID,
                offsetSec = info.offset,
                posOffset = new float2(-5,0),
                posAdjustor = new float2(1,1),
                notePropertyTemps = new NativeArray<NotePropertyTemp>(notePropertyTemps,Allocator.TempJob),
                bpmChanges = new NativeArray<BPMChange>(bpmChanges,Allocator.TempJob),
                noteOptionData = new NoteOptionData()
                {
                    scrollSpeed = noteOptionController.GetScrollSpeed().Value,
                    playerInputOffset = noteOptionController.GetPlayerInputOffset().Value
                }
            });
        }

    }
}