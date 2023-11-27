using System;
using RhythmSystemEntities.ECS.System;
using UniRx;
using Unity.Entities;
using UnityEngine;

namespace RhythmSystemEntities.System.Option
{
    /// <summary>
    /// オプションデータをECS側に送るクラス
    /// </summary>
    public class NoteOptionSystemPresenter : MonoBehaviour
    {
        [SerializeField,Tooltip("オプションデータの参照")] private NoteOptionController noteOptionController;
        [SerializeField,Tooltip("曲ID")] private int musicID;

        //データを送るSystem
        private NoteOptionPresenterSystem noteOptionPresenterSystem;

        private void Start()
        {
            //system取得
            noteOptionPresenterSystem =
                World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<NoteOptionPresenterSystem>();
            
            //パラメータの購読をしてECSに流す
            noteOptionController
                .GetScrollSpeed()
                .Subscribe(scrollSpeed =>
                {
                    float playerInputOffset = noteOptionController.GetPlayerInputOffset().Value;
                    noteOptionPresenterSystem.SetNoteOption(musicID,scrollSpeed,playerInputOffset);
                })
                .AddTo(this);

            //パラメータの購読をしてECSに流す
            noteOptionController
                .GetPlayerInputOffset()
                .Subscribe(playerInputOffset =>
                {
                    float scrollSpeed = noteOptionController.GetScrollSpeed().Value;
                    noteOptionPresenterSystem.SetNoteOption(musicID,scrollSpeed,playerInputOffset);
                })
                .AddTo(this);
        }
    }
}