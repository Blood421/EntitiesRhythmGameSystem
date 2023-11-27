using UniRx;
using UnityEngine;

namespace RhythmSystemEntities.System.Timer
{
    /// <summary>
    /// オーディオデータをタイマーとして扱うクラス
    /// </summary>
    public class MusicTimer : MonoBehaviour
    {
        //タイマーの状態
        public enum MusicState
        {
            beforePlay,
            Playing,
            afterPlay,
        }
        
        [SerializeField,Tooltip("オーディオソース")] private AudioSource audioSource;
        [SerializeField,Tooltip("負の値にしてください,曲の始まりタイミングの調整")] private float offset = 0;
        public float GetOffset() => offset;
        
        //時間の購読用
        private ReactiveProperty<float> time = new ReactiveProperty<float>(0);
        public IReadOnlyReactiveProperty<float> GetTime() => time;

        //今の状態の購読用
        private ReactiveProperty<MusicState> musicState = new ReactiveProperty<MusicState>(MusicState.beforePlay);
        public IReadOnlyReactiveProperty<MusicState> GetMusicState() => musicState;
        
        //オーディオが始まっているかどうか
        private bool isAudioStart = false;
        public bool GetIsAudioStart() => isAudioStart;
        
        //オーディオはポーズ状態かどうか
        private bool isPause = false;
        public bool GetIsPause() => isPause;
        
        //時間を外部から調整するときに使う一時データ
        private float addedTempTime = 0;

        private void Awake()
        {
            Init();
            //オーディオのロード
            audioSource.clip.LoadAudioData();
        }

        private void Start()
        {
            //状態をログに
            musicState
                .Subscribe(state =>
                {
                    Debug.Log(state.ToString());
                })
                .AddTo(this);
        }

        private void Update()
        {
            //ポーズ中なら実行しない
            if (isPause) return;
            //startメソッドが呼ばれなければ実行しない
            if (!isAudioStart) return;

            //オーディオが始まるまでの状態
            if (time.Value < 0 && musicState.Value == MusicState.beforePlay)
            {
                //時間が負,stateがbeforePlayのとき
                //beforePlayの時の処理
                time.Value += Time.deltaTime;
            }
            if (time.Value >= 0 && musicState.Value == MusicState.beforePlay)
            {
                //時間が0以上,stateがbeforePlayのとき
                //Playingに移行する処理
                musicState.Value = MusicState.Playing;
                audioSource.Play();
                time.Value = audioSource.time;
            }

            if (musicState.Value == MusicState.Playing)
            {
                //Playingの時の処理
                time.Value = audioSource.time;
            }

            if (!audioSource.isPlaying && musicState.Value == MusicState.Playing)
            {
                //afterPlay移行する処理
                //多分16ms以下のズレが起きる(60fps)
                time.Value += Time.deltaTime;
                musicState.Value = MusicState.afterPlay;
            }

            if (musicState.Value == MusicState.afterPlay)
            {
                //afterPlayの時の処理
                time.Value += Time.deltaTime;
            }
            
        }

        /// <summary>
        /// 初期化
        /// </summary>
        public void Init()
        {
            Debug.Log("MusicTime初期化");
            musicState.Value = MusicState.beforePlay;
            time.Value = offset;
            addedTempTime = 0;
            isAudioStart = false;
            isPause = false;
            audioSource.Stop();
        }
        
        /// <summary>
        /// 曲スタート
        /// </summary>
        public void MusicStart()
        {
            Debug.Log("MusicTimeスタート");
            isAudioStart = true;
            isPause = false;
            audioSource.UnPause();
        }
        
        /// <summary>
        /// 曲ポーズ
        /// </summary>
        public void MusicPause()
        {
            Debug.Log("MusicTimeポーズ");
            isAudioStart = false;
            isPause = true;
            audioSource.Pause();
        }

        /// <summary>
        /// 曲ストップ
        /// </summary>
        public void MusicStop()
        {
            Debug.Log("MusicTime終了");
            musicState.Value = MusicState.beforePlay;
            Init();
        }

        //TODO:バグがあるらしい 要検証
        public void AddTime(float value)
        {
            //video.urlなのでclipはnull
            //if (videoPlayer.clip == null) return;
            float nowTime = time.Value;
            nowTime += value;
            
            if (nowTime < 0)
            {
                musicState.Value = MusicState.beforePlay;
                time.Value = nowTime;
                audioSource.Stop();
            }
            else
            {
                if (musicState.Value == MusicState.beforePlay)
                {
                    addedTempTime = nowTime;
                    if (!isPause && isAudioStart)
                    {
                        audioSource.Play();
                        audioSource.time = addedTempTime;
                        musicState.Value = MusicState.Playing;
                    }
                }
                else if (musicState.Value == MusicState.Playing)
                {
                    audioSource.time = nowTime;
                }
                else if (musicState.Value == MusicState.afterPlay)
                {
                    addedTempTime = nowTime;
                    if (!isPause && isAudioStart)
                    {
                        audioSource.Play();
                        audioSource.time = addedTempTime;
                        musicState.Value = MusicState.Playing;
                    }
                }
                
                time.Value = nowTime;
            }
        }

        /// <summary>
        /// 時間をセットする
        /// </summary>
        /// <param name="time">時間(sec)</param>
        public void SetTime(float time)
        {
            Debug.Log("時間をセットしたよ : " + time);
            if (time < 0)
            {
                this.time.Value = time;
                musicState.Value = MusicState.beforePlay;
            }else if (time >= 0 && time < audioSource.clip.length)
            {
                audioSource.time = time;
                this.time.Value = time;
                musicState.Value = MusicState.Playing;
            }
            else
            {
                this.time.Value = time;
                musicState.Value = MusicState.afterPlay;
            }
        }

        private void OnDestroy()
        {
            //破棄
            musicState.Dispose();
        }
    }
}