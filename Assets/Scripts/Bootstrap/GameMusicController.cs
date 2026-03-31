using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace GlobalDomination.Managers
{
    /// <summary>
    /// Starts background music automatically — no Inspector setup required.
    /// Priority: optional clip on UITestManager, then Resources/Music, then StreamingAssets/Music/GameMusic.wav (or .ogg).
    /// </summary>
    public sealed class GameMusicController : MonoBehaviour
    {
        private static GameMusicController instance;
        private static AudioClip clipOverride;

        [SerializeField] private AudioClip musicClip;
        [SerializeField] [Range(0f, 1f)] private float volume = 0.42f;
        [SerializeField] private bool loop = true;

        private AudioSource audioSource;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStartBackgroundMusic()
        {
            AudioClip fromUi = null;
            UITestManager utm = Object.FindFirstObjectByType<UITestManager>(FindObjectsInactive.Include);
            if (utm != null)
            {
                fromUi = utm.BackgroundMusicClip;
            }

            EnsurePlaying(fromUi);
        }

        /// <summary>
        /// Creates the music host once. Usually called automatically after the scene loads.
        /// </summary>
        public static void EnsurePlaying(AudioClip optionalClip = null)
        {
            if (instance != null)
            {
                return;
            }

            if (Object.FindFirstObjectByType<GameMusicController>(FindObjectsInactive.Exclude) != null)
            {
                return;
            }

            clipOverride = optionalClip;
            GameObject host = new GameObject("GameMusic");
            host.AddComponent<GameMusicController>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            if (clipOverride != null)
            {
                musicClip = clipOverride;
                clipOverride = null;
            }

            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.loop = loop;
            audioSource.playOnAwake = false;
            audioSource.volume = volume;
            audioSource.spatialBlend = 0f;
            audioSource.priority = 128;
        }

        private IEnumerator Start()
        {
            if (musicClip == null)
            {
                musicClip = LoadFromResources();
            }

            if (musicClip == null)
            {
                yield return LoadFromStreamingAssetsCoroutine();
            }

            if (musicClip == null)
            {
                LogNoMusicHelp();
                yield break;
            }

            if (Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length == 0)
            {
                Debug.LogWarning("[GameMusic] No AudioListener in the scene — add one to your main Camera.");
            }

            audioSource.clip = musicClip;
            audioSource.Play();
        }

        private IEnumerator LoadFromStreamingAssetsCoroutine()
        {
            string folder = Path.Combine(Application.streamingAssetsPath, "Music");
            if (!Directory.Exists(folder))
            {
                yield break;
            }

            string[] names = { "GameMusic.wav", "GameMusic.ogg", "game_music.wav", "game_music.ogg" };
            foreach (string fileName in names)
            {
                string fullPath = Path.Combine(folder, fileName);
                if (!File.Exists(fullPath))
                {
                    continue;
                }

                AudioType audioType = fileName.EndsWith(".ogg", System.StringComparison.OrdinalIgnoreCase)
                    ? AudioType.OGGVORBIS
                    : AudioType.WAV;

                string uri = new System.Uri(fullPath).AbsoluteUri;
                using UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uri, audioType);
                yield return www.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
                if (www.result != UnityWebRequest.Result.Success)
#else
                if (www.isNetworkError || www.isHttpError)
#endif
                {
                    continue;
                }

                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                if (clip != null)
                {
                    musicClip = clip;
                    yield break;
                }
            }
        }

        private static AudioClip LoadFromResources()
        {
            string[] paths =
            {
                "Music/GameMusic",
                "Music/game_music"
            };

            for (int i = 0; i < paths.Length; i++)
            {
                AudioClip clip = Resources.Load<AudioClip>(paths[i]);
                if (clip != null)
                {
                    return clip;
                }
            }

            AudioClip[] inFolder = Resources.LoadAll<AudioClip>("Music");
            if (inFolder != null && inFolder.Length == 1)
            {
                return inFolder[0];
            }

            return null;
        }

        private static void LogNoMusicHelp()
        {
            AudioClip[] inFolder = Resources.LoadAll<AudioClip>("Music");
            if (inFolder != null && inFolder.Length > 1)
            {
                Debug.LogWarning("[GameMusic] Multiple clips in Resources/Music. Keep one file or name it GameMusic.");
            }
            else
            {
                Debug.LogWarning(
                    "[GameMusic] No music found. Drop GameMusic.wav into Assets/StreamingAssets/Music/ (zero setup), " +
                    "or Assets/Resources/Music/, or assign Background Music on UITestManager.");
            }
        }
    }
}
