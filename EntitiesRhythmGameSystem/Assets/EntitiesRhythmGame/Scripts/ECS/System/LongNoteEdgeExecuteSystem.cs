using RhythmSystemEntities.ECS.Aspect;
using RhythmSystemEntities.ECS.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace RhythmSystemEntities.ECS.System
{
    /// <summary>
    /// ロングノーツの端のエンティティのデータを更新するシステム
    /// </summary>
    [BurstCompile]
    [DisableAutoCreation]
    public partial struct LongNoteEdgeExecuteSystem : ISystem
    {
        //ロングノーツの端データの仮入用
        public struct LongNoteDataToExecuteEdge
        {
            public bool isExecuted;
            public int musicID;
            public int edgeID;
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            //ロングノーツの端のデータをノーツから貰って入れておくリスト
            NativeList<LongNoteDataToExecuteEdge> longNoteDataToExecuteEdgeList =
                new NativeList<LongNoteDataToExecuteEdge>(500, Allocator.TempJob);

            //ロングノーツの端データをノーツから取得するjobの発行
            state.Dependency = new GetLongNoteDataJob()
            {
                longNoteDataToExecuteEdgeList = longNoteDataToExecuteEdgeList,
            }.Schedule(state.Dependency);
            state.Dependency.Complete();
            
            //ロングノーツの端データにノーツから受けとったデータを入れるjobの発行
            state.Dependency = new LongNoteEdgeExecuteJob()
            {
                longNoteDataToExecuteEdgeList = longNoteDataToExecuteEdgeList
            }.Schedule(state.Dependency);
            state.Dependency.Complete();
            
            longNoteDataToExecuteEdgeList.Dispose();
        }
    }
    
    /// <summary>
    /// ロングノーツの端データをノーツから取得するjob
    /// </summary>
    [BurstCompile]
    public partial struct GetLongNoteDataJob : IJobEntity
    {
        public NativeList<LongNoteEdgeExecuteSystem.LongNoteDataToExecuteEdge> longNoteDataToExecuteEdgeList;
        
        [BurstCompile]
        private void Execute(NoteAspect noteAspect)
        {
            //ロングノーツじゃなかったらスキップ
            if (noteAspect.GetNoteType() != NoteComponentData.NoteType.Long) return;
            
            //始点
            longNoteDataToExecuteEdgeList.Add(new LongNoteEdgeExecuteSystem.LongNoteDataToExecuteEdge()
            {
                isExecuted = noteAspect.GetIsExecuted(),
                musicID = noteAspect.GetMusicID(),
                edgeID = noteAspect.GetLongNoteEdgeIDData().beginID
            });
            //終点
            longNoteDataToExecuteEdgeList.Add(new LongNoteEdgeExecuteSystem.LongNoteDataToExecuteEdge()
            {
                isExecuted = noteAspect.GetIsExecuted(),
                musicID = noteAspect.GetMusicID(),
                edgeID = noteAspect.GetLongNoteEdgeIDData().endID
            });
        }
    }
    
    /// <summary>
    /// ロングノーツの端データにノーツから受けとったデータを入れるjob
    /// </summary>
    [BurstCompile]
    public partial struct LongNoteEdgeExecuteJob : IJobEntity
    {
        public NativeList<LongNoteEdgeExecuteSystem.LongNoteDataToExecuteEdge> longNoteDataToExecuteEdgeList;
        
        [BurstCompile]
        private void Execute(LongNoteEdgeAspect longNoteEdgeAspect)
        {
            for (int i = 0; i < longNoteDataToExecuteEdgeList.Length; i++)
            {
                //MusicIDが違ったらスキップ
                //EdgeIDが違ったらスキップ
                //両方一致したら処理してreturn
                if (longNoteDataToExecuteEdgeList[i].musicID != longNoteEdgeAspect.GetMusicID()) continue;
                if (longNoteDataToExecuteEdgeList[i].edgeID != longNoteEdgeAspect.GetEdgeID()) continue;
                longNoteEdgeAspect.SetIsExecuted(longNoteDataToExecuteEdgeList[i].isExecuted);
                return;
            }
        }
    }
}