using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Threading.Tasks;
using RhythmSystemEntities.Data;
using RhythmSystemEntities.ECS.Data;
using UnityEngine;

namespace RhythmSystemEntities.System.Loader
{
    /// <summary>
    /// DJBM形式の譜面をロードするクラス
    /// </summary>
    public class NotesGenerateDataLoaderDJBM
    {
        //ヘッダーの正規表現
        private static readonly List<string> headerPatterns = new List<string>
        {
            @"#(GENRE) (.*)",
            @"#(TITLE) (.*)",
            @"#(SUBTITLE) (.*)",
            @"#(ARTIST) (.*)",
            @"#(SUBARTIST) (.*)",
            @"#(NOTESARTIST) (.*)",
            @"#(BPM) (.*)",
            @"#(RANK) (.*)",
            @"#(PLAYLEVEL) (.*)",
            @"(OFFSET) (.*)",
            @"(COMMENT) ""(.*)""",
        };

        //ファイルのパス
        private string filePath = "";

        //ヘッダー名をキー、データを値とする辞書
        private Dictionary<string, string> headerData = new Dictionary<string, string>();

        //ノーツ情報
        private List<NotePropertyTemp> noteProperties = new List<NotePropertyTemp>();

        //BPM変化情報
        private List<BPMChange> bpmChanges = new List<BPMChange>();

        //各レーンで最後のロングノーツがONになったbeat
        private float[] longNoteBeginBuffers = new float[] {-1, -1, -1, -1}; //4レーン

        //曲情報
        private MusicInfo musicInfo;

        //メインデータ読み取り開始用
        private bool isMainStart = false;

        //読み込むレーンのカウント
        private int laneCount = 0;

        //総合ビート
        private float beat = 0;

        //コンストラクタ ファイルを読み込む
        public NotesGenerateDataLoaderDJBM(string filePath)
        {
            this.filePath = filePath;
        }

        
        /// <summary>
        /// 譜面のロード
        /// </summary>
        public void Load()
        {
            //ファイルを読み込み、各行を配列に保持
            string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);
            //ヘッダー読み込み
            foreach (string line in lines)
            {
                LoadHeaderLine(line);
            }

            //曲情報を生成
            //TODO:必要なら例外処理
            musicInfo = new MusicInfo(
                GetHeaderDataValue("GENRE"),
                GetHeaderDataValue("TITLE"),
                GetHeaderDataValue("SUBTITLE"),
                GetHeaderDataValue("ARTIST"),
                GetHeaderDataValue("SUBARTIST"),
                GetHeaderDataValue("NOTESARTIST"),
                Convert.ToSingle(GetHeaderDataValue("BPM")),
                Convert.ToInt32(GetHeaderDataValue("RANK")),
                Convert.ToInt32(GetHeaderDataValue("PLAYLEVEL")),
                Convert.ToSingle(GetHeaderDataValue("OFFSET")),
                GetHeaderDataValue("COMMENT")
            );

            //基本BPMをbeat0のBPMに設定
            bpmChanges.Add(new BPMChange()
            {
                afterBPM = Convert.ToSingle(headerData["BPM"]),
                executeBeat = 0
            });
            
            //メインデータの読み込み
            foreach (string line in lines)
            {
                LoadMainDataLine(line);
            }

            //テンポ変化データを時系列旬に並び替え
            bpmChanges = bpmChanges.OrderBy(x => x.executeBeat).ToList();

        }
        
        /// <summary>
        /// 譜面の非同期ロード
        /// テキスト読み取りの行數がoneFrameExecuteNum超えたら1Frame待機
        /// </summary>
        /// <param name="oneFrameExecuteNum">何行読み取りで待機するか</param>
        /// <param name="token">キャンセレーショントークン</param>
        public async UniTask LoadAsync(int oneFrameExecuteNum,CancellationToken token)
        {
            //ファイルを読み込み、各行を配列に保持
            string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);
            //ヘッダー読み込み
            for (var i = 0; i < lines.Length; i++)
            {
                //指定行數行ったら1フレ待機
                if (i % oneFrameExecuteNum == 0) await UniTask.DelayFrame(1,cancellationToken:token);
                
                string line = lines[i];
                LoadHeaderLine(line);
            }

            //曲情報を生成
            //TODO:必要なら例外処理
            musicInfo = new MusicInfo(
                GetHeaderDataValue("GENRE"),
                GetHeaderDataValue("TITLE"),
                GetHeaderDataValue("SUBTITLE"),
                GetHeaderDataValue("ARTIST"),
                GetHeaderDataValue("SUBARTIST"),
                GetHeaderDataValue("NOTESARTIST"),
                Convert.ToSingle(GetHeaderDataValue("BPM")),
                Convert.ToInt32(GetHeaderDataValue("RANK")),
                Convert.ToInt32(GetHeaderDataValue("PLAYLEVEL")),
                Convert.ToSingle(GetHeaderDataValue("OFFSET")),
                GetHeaderDataValue("COMMENT")
            );

            //基本BPMをbeat0のBPMに設定
            bpmChanges.Add(new BPMChange()
            {
                afterBPM = Convert.ToSingle(headerData["BPM"]),
                executeBeat = 0
            });
            //メインデータ読み込み
            for (var i = 0; i < lines.Length; i++)
            {
                //指定行數行ったら1フレ待機
                if (i % oneFrameExecuteNum == 0) await UniTask.DelayFrame(1,cancellationToken:token);
                
                string line = lines[i];
                LoadMainDataLine(line);
            }

            //テンポ変化データを時系列旬に並び替え
            bpmChanges = bpmChanges.OrderBy(x => x.executeBeat).ToList();
        }

        //ヘッダー行のみ読み込む
        private void LoadHeaderLine(string line)
        {
            //各ヘッダー名に対して実行
            foreach (var headerPattern in headerPatterns)
            {
                Match match = Regex.Match(line, headerPattern);
                //ヘッダー行のパターンに一致すればデータ取得
                if (match.Success)
                {
                    //ヘッダー名
                    string headerName = match.Groups[1].Value;
                    //データ本体
                    string data = match.Groups[2].Value;
                    headerData[headerName] = data;
                    return;
                }
            }
        }

        //メインデータの行のみ読み込む
        private void LoadMainDataLine(string line)
        {
            //行読み込み

            //コメントチェック
            if (CommentPatternExistCheck(line))
            {
                //コメントなのでスキップ
                return;
            }

            //メソッドチェック
            if (MethodPatternExistCheck(line))
            {
                //メソッド処理
                ExecuteMethod(line);
                return;
            }

            //メインがスタートされていなかったらスキップ
            if (!isMainStart) return;

            if (laneCount == 0)
            {
                //ギミックレーン
                LoadGimmickLaneData(line, beat);
                return;
            }

            //通常レーン
            LoadMainLaneData(line, beat);
        }

        //通常レーンの行読み込み
        private void LoadMainLaneData(string line, float nowBeat)
        {
            //この小節の数字の数
            //3までしか使ってない
            //0個の可能性もある
            Match numMatch = Regex.Match(line, @"[0-3]*");
            int numCount = numMatch.Value.Length;
            //この小節の1つの数字のbeat
            float oneBeat = 4f / numCount;

            for (var i = 0; i < line.Length; i++)
            {
                //文字処理
                LoadMainDataCharacter(line[i], oneBeat * i + nowBeat);

                if (line[i] == ',')
                {
                    //","でレーンを終わらせる
                    laneCount++;
                }

                if (line[i] == ';')
                {
                    //レーンをギミックに戻す
                    laneCount = 0;
                    //TODO:小節終わり処理
                    //小節終わりなのでビートを4足す
                    beat += 4;
                }
            }
        }

        //メインデータの行の文字のみ読み込む
        private void LoadMainDataCharacter(char character, float nowBeat)
        {
            //空白
            if (character == '0') return;

            //シングルノーツ
            if (character == '1')
            {
                //ギミックレーンがあるからlaneCount-1
                noteProperties.Add(new NotePropertyTemp()
                {
                    beginBeat = nowBeat,
                    endBeat = nowBeat,
                    lane = laneCount - 1,
                    noteType = NoteComponentData.NoteType.Single
                });
                return;
            }

            //ロングノーツ(始点)
            if (character == '2')
            {
                //ギミックレーンがあるからlaneCount-1
                longNoteBeginBuffers[laneCount - 1] = nowBeat;
                return;
            }

            //ロングノーツ(終点)
            if (character == '3')
            {
                //ギミックレーンがあるからlaneCount-1
                float begin = longNoteBeginBuffers[laneCount - 1];
                float end = nowBeat;
                noteProperties.Add(new NotePropertyTemp()
                {
                    beginBeat = begin,
                    endBeat = end,
                    lane = laneCount - 1,
                    noteType = NoteComponentData.NoteType.Long
                });
                return;
            }
        }

        //ギミックレーンの行読み込み
        private void LoadGimmickLaneData(string line, float nowBeat)
        {
            //まず桁数を確認
            string[] split = line.Split(new[]
            {
                'B',
                'S',
            });

            //桁数
            //4beat / zeroCountが"0"1つのbeat
            int zeroCount = 0;

            for (var i = 0; i < split.Length; i++)
            {
                if (i % 2 == 0)
                {
                    Match match;
                    match = Regex.Match(split[i], @"0*");

                    if (match.Success)
                    {
                        //0の数を数えて足す
                        int zero = match.Value.Count(n => n == '0');
                        zeroCount += zero;

                    }
                }
            }

            //1つの"0"のbeat
            float oneBeat = 4f / zeroCount;

            //BPM変化の場合
            string[] splitB = line.Split('B');
            //一時保持
            float changeBPM = 0;
            //0のカウント
            zeroCount = 0;
            for (var i = 0; i < splitB.Length; i++)
            {
                if (i % 2 == 1)
                {
                    Match match;
                    match = Regex.Match(splitB[i], @"\d+(\.\d+)?");

                    if (match.Success)
                    {
                        //BPMを変化させる
                        changeBPM = float.Parse(match.Value);

                        //次の0のときの0の数
                        int zero = zeroCount;
                        if(oneBeat > float.MaxValue) oneBeat = 0;

                        float beat = zero * oneBeat + nowBeat;

                        //bpmChangesに登録
                        bpmChanges.Add(new BPMChange()
                        {
                            afterBPM = changeBPM,
                            executeBeat = beat
                        });
                    }
                }
                else
                {
                    Match match;
                    match = Regex.Match(split[i], @"0*");
                    if (match.Success)
                    {
                        //0の数を数えて足す
                        int zero = match.Value.Count(n => n == '0');
                        zeroCount += zero;
                    }
                }
            }

            //スクロールの場合
            //TODO:後でスクロール実装時に使うので取っておく
            string[] splitS = line.Split('S');

            for (var i = 0; i < splitS.Length; i++)
            {
                if (i % 2 == 1)
                {
                    Match match;
                    match = Regex.Match(splitS[i], @"\d+(\.\d+)?");

                    if (match.Success)
                    {
                        Debug.Log(match.Value);
                    }
                }
            }
            if (line.Contains(','))
            {
                //","でレーンを終わらせる
                laneCount++;
            }
        }

        //##から始まるものを"メソッド"と定義する
        //それのチェック
        private bool MethodPatternExistCheck(string line)
        {
            //長さチェックもする
            if (line.Length < 2) return false;
            string check = line[0].ToString() + line[1].ToString();
            Match match = Regex.Match(check, @"#{2}");
            return match.Success;
        }

        //###から始まるものを"コメント"と定義する
        //それのチェック
        private bool CommentPatternExistCheck(string line)
        {
            //長さチェックもする
            if (line.Length < 3) return false;
            string check = line[0].ToString() + line[1].ToString() + line[2].ToString();
            Match match = Regex.Match(check, @"#{3}");
            return match.Success;
        }

        //メソッドを処理
        private void ExecuteMethod(string line)
        {
            Match match;

            match = Regex.Match(line, @"##START");
            if (match.Success)
            {
                //スタート処理
                isMainStart = true;
                return;
            }

            match = Regex.Match(line, @"##END");
            if (match.Success)
            {
                //スタート処理
                isMainStart = false;
                return;
            }

            match = Regex.Match(line, @"(##RHYTHM) (.*)");
            if (match.Success)
            {
                //TODO:拍子変化処理
                return;
            }
        }

        //headerDataの値 Get
        private string GetHeaderDataValue(string key)
        {
            if (!headerData.ContainsKey(key))
            {
                throw new Exception("HeaderDataで例外発生 : " + key);
            }

            return headerData[key];
        }

        //各種ロードデータの取得メソッド
        public List<NotePropertyTemp> GetNoteProperties() => noteProperties;
        public List<BPMChange> GetBPMChanges() => bpmChanges;
        public MusicInfo GetMusicInfo() => musicInfo;
    }
}