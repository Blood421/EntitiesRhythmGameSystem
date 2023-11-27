using System;
using System.Collections.Generic;
using RhythmSystemEntities.Data;
using RhythmSystemEntities.ECS.Aspect;
using UniRx;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace RhythmSystemEntities.ECS.System
{
    /// <summary>
    /// ノーツの入力判定と通り過ぎ判定をするシステム
    /// </summary>
    [UpdateAfter(typeof(NotePresenterSystem))]
    [DisableAutoCreation]
    public partial class NoteJudgeSystem : SystemBase
    {
        //判定に使うノーツのデータの仮入れ
        public struct JudgeTempData
        {
            public int musicID;
            public int lane;
            public int entityIndex;
            public float judgeSec;
            public float offsetSec;
            public bool isProcessing;
        }
        
        //判定結果のデータを入れておく
        public struct JudgeResultData
        {
            public int musicID;
            public int entityIndex;
            public bool isExecuted;
            public float3 noteJudgedPos;
            public GameSettings.JudgeType judgeType;
        }
        
        //通り過ぎ判定の結果を入れておく
        public struct ThroughJudgeResultData
        {
            public int musicID;
            public int lane;
            public bool isExecuted;
            public float3 noteJudgedPos;
            public GameSettings.JudgeType judgeType;
        }
        
        //判定に使う入力データ
        public struct JudgedInputData
        {
            public int musicID;
            public bool isJudge;
            public bool isPressed;
        }
        
        //判定の設定
        public struct JudgeMusicSettings
        {
            public int musicID;
            public bool isAuto;
        }
        
        //判定時に通知するデータ
        public struct JudgeNotifyData
        {
            public int musicID;
            public int lane;
            public bool isExecuted;
            public float3 noteJudgedPos;
            public GameSettings.JudgeType judgeType;
        }

        //通知用
        private Subject<JudgeNotifyData> onJudge;
        public IObservable<JudgeNotifyData> OnJudge() => onJudge;

        //曲毎の判定設定
        private List<JudgeMusicSettings> judgeMusicSettingsList;
        
        //入力があったときのフラグ
        private bool isJudgeInput;
        
        //入力データ
        private NativeArray<JudgedInputData> judgedInputData;

        protected override void OnCreate()
        {
            base.OnCreate();
            //使うデータの初期化
            judgeMusicSettingsList = new List<JudgeMusicSettings>(10);
            onJudge = new Subject<JudgeNotifyData>();
            isJudgeInput = false;
        }

        //Updateで呼ぶ
        protected override void OnUpdate()
        {
            //ノーツの通り過ぎ処理をした結果を入れるリスト
            NativeList<ThroughJudgeResultData> throughJudgeResultDataList =
                new NativeList<ThroughJudgeResultData>(1000, Allocator.TempJob);
            
            for (int i = 0; i < judgeMusicSettingsList.Count; i++)
            {
                //ノーツの通り過ぎを検知して処理するjobの発行
                Dependency = new ThroughNoteJudgeJob()
                {
                    throughJudgeResultDataList = throughJudgeResultDataList,
                    musicID = judgeMusicSettingsList[i].musicID,
                    isAuto = judgeMusicSettingsList[i].isAuto,
                }.Schedule(Dependency);
                Dependency.Complete();
            }
                
            for (int i = 0; i < throughJudgeResultDataList.Length; i++)
            {
                //MusicIDが無効ならスキップ
                int musicID = throughJudgeResultDataList[i].musicID;
                if(musicID < 0) continue;
                int lane = throughJudgeResultDataList[i].lane;
                bool isExecuted = throughJudgeResultDataList[i].isExecuted;
                GameSettings.JudgeType judgeType = throughJudgeResultDataList[i].judgeType;
                float3 noteJudgedPos = throughJudgeResultDataList[i].noteJudgedPos;
                
                //通知
                onJudge.OnNext(new JudgeNotifyData()
                {
                    musicID = musicID,
                    lane = lane,
                    isExecuted = isExecuted,
                    noteJudgedPos = noteJudgedPos,
                    judgeType = judgeType
                });
            }

            //破棄
            throughJudgeResultDataList.Dispose();
            
            //入力がなかったらスキップ
            if (!isJudgeInput) return;
            //判定
            ExecuteJudge(judgedInputData);
            isJudgeInput = false;

        }

        //破棄
        protected override void OnDestroy()
        {
            base.OnDestroy();
            onJudge.Dispose();
        }

        //曲設定追加
        public void AddMusicSettings(JudgeMusicSettings musicSettings) => judgeMusicSettingsList.Add(musicSettings);

        //MonoのUpdateで呼ぶ
        public void Judge(NativeArray<JudgedInputData> judgedInputData)
        {
            this.judgedInputData = judgedInputData;
            isJudgeInput = true;
        }

        private void ExecuteJudge(NativeArray<JudgedInputData> judgedInputData)
        {
            //判定するsec範囲に入っているノーツのリスト
            NativeList<JudgeTempData> judgeTempDataList =
                new NativeList<JudgeTempData>(GameSettings.laneNum, Allocator.TempJob);

            //判定するsec範囲に入っていたらリストに追加するJobを発行
            Dependency = new WithinJudgeRangeCheckJob()
            {
                judgeTempDataList = judgeTempDataList
            }.Schedule(Dependency);
            Dependency.Complete();

            //判定するsec範囲に入っているノーツのうち判定に使うものを入れるリスト
            NativeArray<JudgeTempData> judgeDecideNoteData = new NativeArray<JudgeTempData>(GameSettings.laneNum,Allocator.TempJob);
            //判定するノーツを決めるJobの発行
            Dependency = new JudgeNoteDecideJob()
            {
                judgeTempDataList = judgeTempDataList,
                judgeDecideNoteData = judgeDecideNoteData,
                judgedInputData = judgedInputData
            }.Schedule(Dependency);
            Dependency.Complete();
            
            //判定の結果を入れるリスト
            NativeArray<JudgeResultData> judgeResultData = new NativeArray<JudgeResultData>(GameSettings.laneNum,Allocator.TempJob);
            for (int lane = 0; lane < judgeResultData.Length; lane++)
            {
                //デフォルトのデータを入れておく
                //これの場合判定していない
                judgeResultData[lane] = new JudgeResultData()
                {
                    musicID = -1,
                    entityIndex = Int32.MinValue,
                    isExecuted = false,
                    noteJudgedPos = new float3(-10000,-10000,-10000),
                    judgeType = GameSettings.JudgeType.Miss,
                };
            }
            
            //入力データから判定するJobを発行
            Dependency = new InputJudgeJob()
            {
                judgeResultData = judgeResultData,
                judgeDecideNoteData = judgeDecideNoteData,
                judgedInputData = judgedInputData
            }.Schedule(Dependency);
            Dependency.Complete();
            
            //判定結果をノーツに反映させるJobを発行
            Dependency = new JudgedResultToNoteJob()
            {
                judgeResultData = judgeResultData,
            }.Schedule(Dependency);
            Dependency.Complete();

            //結果を通知させる
            for (int lane = 0; lane < judgeResultData.Length; lane++)
            {
                //MusicIDが無効ならスキップ
                int musicID = judgeResultData[lane].musicID;
                if(musicID < 0) continue;
                
                bool isExecuted = judgeResultData[lane].isExecuted;
                GameSettings.JudgeType judgeType = judgeResultData[lane].judgeType;
                float3 noteJudgedPos = judgeResultData[lane].noteJudgedPos;
                
                //通知
                onJudge.OnNext(new JudgeNotifyData()
                {
                    musicID = musicID,
                    lane = lane,
                    isExecuted = isExecuted,
                    noteJudgedPos = noteJudgedPos,
                    judgeType = judgeType
                });
            }

            //破棄
            judgeTempDataList.Dispose();
            judgeDecideNoteData.Dispose();
            judgeResultData.Dispose();
            judgedInputData.Dispose();
        }
    }

    /// <summary>
    /// ノーツの通り過ぎを検知して処理するjob
    /// </summary>
    [BurstCompile]
    public partial struct ThroughNoteJudgeJob : IJobEntity
    {
        public int musicID;
        public bool isAuto;
        public NativeList<NoteJudgeSystem.ThroughJudgeResultData> throughJudgeResultDataList;
        
        [BurstCompile]
        private void Execute(NoteAspect noteAspect)
        {
            //曲IDが違ったらスキップ
            if(noteAspect.GetMusicID() != musicID) return;
            if (noteAspect.ThroughNote(isAuto))
            {
                //ノーツを処理した
                //オートならパーフェクト,オートじゃないなら処理
                
                GameSettings.JudgeType judgeType = GameSettings.JudgeType.Miss;
                if (isAuto) judgeType = GameSettings.JudgeType.Perfect;
                
                //判定位置取得
                float3 noteJudgedPos = noteAspect.GetNoteJudgedPos();
                
                //リストに追加
                throughJudgeResultDataList.Add(new NoteJudgeSystem.ThroughJudgeResultData()
                {
                    musicID = musicID,
                    lane = noteAspect.GetLane(),
                    isExecuted = noteAspect.GetIsExecuted(),
                    noteJudgedPos = noteJudgedPos,
                    judgeType = judgeType,
                });
            }
        }
    }
    
    #region JudgeJobs
    
    /// <summary>
    /// 判定するsec範囲に入っていたらリストに追加するJob
    /// </summary>
    [BurstCompile]
    public partial struct WithinJudgeRangeCheckJob : IJobEntity
    {
        public NativeList<NoteJudgeSystem.JudgeTempData> judgeTempDataList;
        
        [BurstCompile]
        private void Execute(NoteAspect noteAspect)
        {
            //判定sec範囲に入っていなかったらスキップ
            if (!noteAspect.WithinJudgeRangeCheck()) return;
            int index = noteAspect.GetEntityIndex();
            float judgeSec = noteAspect.GetJudgeSec();
            //オフセットはまとめる
            //プレイヤーからの入力オフセットと譜面の判定オフセット
            float offsetSec = noteAspect.GetJudgeOffsetSec() - noteAspect.GetPlayerInputOffsetSec();
            bool isProcessing = noteAspect.GetIsProcessing();
            int lane = noteAspect.GetLane();
            int musicID = noteAspect.GetMusicID();
            
            //リストに追加
            judgeTempDataList.Add(new NoteJudgeSystem.JudgeTempData()
            {
                musicID = musicID,
                lane = lane,
                entityIndex = index,
                judgeSec = judgeSec,
                offsetSec = offsetSec,
                isProcessing = isProcessing
            });
        }
    }
    
    /// <summary>
    /// 判定するノーツを決めるJob
    /// </summary>
    [BurstCompile]
    public partial struct JudgeNoteDecideJob : IJobEntity
    {
        public NativeList<NoteJudgeSystem.JudgeTempData> judgeTempDataList;
        public NativeArray<NoteJudgeSystem.JudgeTempData> judgeDecideNoteData;
        public NativeArray<NoteJudgeSystem.JudgedInputData> judgedInputData;

        [BurstCompile]
        private void Execute(NotePresenterAspect notePresenterAspect)
        {
            //範囲内で一番低いものを選択
            //なかった場合ありえない値を入れる
            //IDが違ってたらスキップ
            if (notePresenterAspect.GetMusicID() != judgedInputData[0].musicID) return;

            //デフォルトデータ
            for (int lane = 0; lane < judgeDecideNoteData.Length; lane++)
            {                    
                judgeDecideNoteData[lane] = new NoteJudgeSystem.JudgeTempData()
                {
                    musicID = -1,
                    lane = -1,
                    entityIndex = Int32.MinValue,
                    judgeSec = 99999,
                    offsetSec = 0,
                    isProcessing = false
                };
            }

            //データの決定
            for (int i = 0; i < judgeTempDataList.Length; i++)
            {
                int lane = judgeTempDataList[i].lane;
                if (!judgedInputData[lane].isJudge) continue;
                
                NoteJudgeSystem.JudgeTempData data = judgeTempDataList[i];
                //secが一番小さいものに更新していく
                if(data.judgeSec < judgeDecideNoteData[lane].judgeSec) judgeDecideNoteData[lane] = data; 
                
            }
        }
    }
    
    /// <summary>
    /// 入力データから判定するJob
    /// </summary>
    [BurstCompile]
    public partial struct InputJudgeJob : IJobEntity
    {
        //***リザルトデータはまだ完成しない***
        public NativeArray<NoteJudgeSystem.JudgeResultData> judgeResultData;
        public NativeArray<NoteJudgeSystem.JudgeTempData> judgeDecideNoteData;
        public NativeArray<NoteJudgeSystem.JudgedInputData> judgedInputData;
        
        [BurstCompile]
        private void Execute(NotePresenterAspect notePresenterAspect)
        {
            //曲IDが違ったらスキップ
            if (notePresenterAspect.GetMusicID() != judgedInputData[0].musicID) return; 
            
            float nowSec = notePresenterAspect.GetNowSec();
            float judgeNowSec = 0;
            float missRange = GameSettings.missRange;
            float goodRange = GameSettings.goodRange;
            float perfectRange = GameSettings.perfectRange;
            float judgeSec = 0;
            float secDif = 0;
            
            for (int lane = 0; lane < judgeDecideNoteData.Length; lane++)
            {
                //無効データなのでスキップ
                if(judgeDecideNoteData[lane].musicID != notePresenterAspect.GetMusicID()) continue;
                //そもそも判定しないレーンはスキップ
                if(!judgedInputData[lane].isJudge) continue;
                
                //押されたとき
                if (judgedInputData[lane].isPressed)
                {
                    //この場合は例外なのでスキップ
                    if(judgeDecideNoteData[lane].isProcessing) continue;

                    judgeSec = judgeDecideNoteData[lane].judgeSec;
                    judgeNowSec = nowSec - judgeDecideNoteData[lane].offsetSec;

                    secDif =  Mathf.Abs(judgeSec - judgeNowSec);
                    //パーフェクトの範囲内
                    if(secDif < perfectRange)
                    {
                        judgeResultData[lane] = new NoteJudgeSystem.JudgeResultData()
                        {
                            musicID = judgeDecideNoteData[lane].musicID,
                            entityIndex = judgeDecideNoteData[lane].entityIndex,
                            judgeType = GameSettings.JudgeType.Perfect
                        };
                        continue;
                    }
                    //Goodの範囲内
                    if(secDif < goodRange)
                    {
                        judgeResultData[lane] = new NoteJudgeSystem.JudgeResultData()
                        {
                            musicID = judgeDecideNoteData[lane].musicID,
                            entityIndex = judgeDecideNoteData[lane].entityIndex,
                            judgeType = GameSettings.JudgeType.Good
                        };
                        continue;
                    }

                    //上記範囲内でなければミス
                    judgeResultData[lane] = new NoteJudgeSystem.JudgeResultData()
                    {
                        musicID = judgeDecideNoteData[lane].musicID,
                        entityIndex = judgeDecideNoteData[lane].entityIndex,
                        judgeType = GameSettings.JudgeType.Miss
                    };
                    
                    continue;
                }
                
                //離れたとき
                //離れたときに処理中でないノーツはスキップ
                if(!judgeDecideNoteData[lane].isProcessing) continue;
                judgeSec = judgeDecideNoteData[lane].judgeSec;
                judgeNowSec = nowSec - judgeDecideNoteData[lane].offsetSec;

                secDif =  Mathf.Abs(judgeSec - judgeNowSec);
                //パーフェクトの範囲内
                if(secDif < perfectRange)
                {
                    judgeResultData[lane] = new NoteJudgeSystem.JudgeResultData()
                    {
                        musicID = judgeDecideNoteData[lane].musicID,
                        entityIndex = judgeDecideNoteData[lane].entityIndex,
                        judgeType = GameSettings.JudgeType.Perfect
                    };
                    continue;
                }
                //Goodの範囲内
                if(secDif < goodRange)
                {
                    judgeResultData[lane] = new NoteJudgeSystem.JudgeResultData()
                    {
                        musicID = judgeDecideNoteData[lane].musicID,
                        entityIndex = judgeDecideNoteData[lane].entityIndex,
                        judgeType = GameSettings.JudgeType.Good
                    };
                    continue;
                }
                
                //上記範囲内でなければミス
                judgeResultData[lane] = new NoteJudgeSystem.JudgeResultData()
                {
                    musicID = judgeDecideNoteData[lane].musicID,
                    entityIndex = judgeDecideNoteData[lane].entityIndex,
                    judgeType = GameSettings.JudgeType.Miss
                };
            }
        }
    }

    /// <summary>
    /// 判定結果をノーツに反映させるJob
    /// </summary>
    [BurstCompile]
    public partial struct JudgedResultToNoteJob : IJobEntity
    {
        public NativeArray<NoteJudgeSystem.JudgeResultData> judgeResultData;
        [BurstCompile]
        private void Execute(NoteAspect noteAspect)
        {
            int lane = noteAspect.GetLane();
            //MusicIDが一致していなかったらスキップ
            //エンティティが一致していなかったらスキップ
            if (judgeResultData[lane].musicID != noteAspect.GetMusicID()) return;
            if (judgeResultData[lane].entityIndex != noteAspect.GetEntityIndex()) return;
            
            //ノーツを処理済みにする
            noteAspect.ExecuteNote(judgeResultData[lane].judgeType);
            //判定位置取得
            float3 judgedPos = noteAspect.GetNoteJudgedPos();
            
            //リザルトデータ完成
            judgeResultData[lane] = new NoteJudgeSystem.JudgeResultData()
            {
                musicID = judgeResultData[lane].musicID,
                isExecuted = noteAspect.GetIsExecuted(),
                entityIndex = judgeResultData[lane].entityIndex,
                noteJudgedPos = judgedPos,
                judgeType = judgeResultData[lane].judgeType,
            };
        }
    }
    
    #endregion

}