using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public class DialogLine
{
    [TextArea(2, 6)] public string text;
    public Sprite leftPortrait;
    public Sprite rightPortrait;
    public AudioClip typeSound;
}

[Serializable]
public class LevelDefinition
{
    public string sceneName;
    public List<DialogLine> openingDialog = new List<DialogLine>();
}

public class LevelManager : MonoBehaviour
{
    private static LevelManager instance;

    [Header("Flow")]
    [SerializeField] private string launcherSceneName = "Launcher";
    [SerializeField] private string endingSceneName = "ED";
    [SerializeField] private List<LevelDefinition> levels = new List<LevelDefinition>();

    [Header("Dialog")]
    [SerializeField] private GameObject dialogPrefab;

    [Header("Black Transition")]
    [SerializeField] private float completionDelay = 2f;
    [SerializeField] private float fadeInDuration = 1f;
    [SerializeField] private float blackHoldDuration = 3f;
    [SerializeField] private float fadeOutDuration = 1f;

    [Header("Music")]
    [SerializeField] private AudioClip levelMusic;
    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 1f;

    private readonly List<EnemyController> enemies = new List<EnemyController>();
    private AudioSource musicSource;
    private int pendingLevelIndex = -1;
    private int currentLevelIndex = -1;
    private int aliveEnemies;
    private bool waitingForFade;
    private bool levelActive;
    private bool levelCompleting;
    private bool skipOpeningDialog;

    public static LevelManager Instance => instance;
    public int CurrentLevelIndex => currentLevelIndex;
    public bool IsLevelActive => levelActive;
    public GameObject DialogPrefab => dialogPrefab;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += HandleSceneLoaded;

        musicSource = GetComponent<AudioSource>();
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
        }

        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            instance = null;
        }
    }

    private void Start()
    {
        if (SceneManager.GetActiveScene().name != launcherSceneName)
        {
            return;
        }

        if (levels.Count == 0)
        {
            Debug.LogWarning("LevelManager: no levels configured.");
            return;
        }

        StartLevel(0, false);
    }

    private void StartLevel(int index, bool useFade)
    {
        if (index < 0 || index >= levels.Count)
        {
            return;
        }

        pendingLevelIndex = index;
        waitingForFade = useFade;
        skipOpeningDialog = false;

        if (useFade)
        {
            PlayTransition(levels[index].sceneName, HandleFadeComplete);
        }
        else
        {
            SceneManager.LoadScene(levels[index].sceneName);
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (pendingLevelIndex < 0 || pendingLevelIndex >= levels.Count)
        {
            return;
        }

        if (scene.name != levels[pendingLevelIndex].sceneName)
        {
            return;
        }

        currentLevelIndex = pendingLevelIndex;
        pendingLevelIndex = -1;
        PrepareLevel();

        if (!waitingForFade)
        {
            if (skipOpeningDialog)
            {
                skipOpeningDialog = false;
                StartGameplay();
            }
            else
            {
                BeginLevel();
            }
        }
    }

    private void HandleFadeComplete()
    {
        waitingForFade = false;
        BeginLevel();
    }

    private void PrepareLevel()
    {
        levelActive = false;
        levelCompleting = false;
        aliveEnemies = 0;
        enemies.Clear();

        SetActorsFrozen(true);

        EnemyController[] found = FindObjectsOfType<EnemyController>();
        foreach (EnemyController enemy in found)
        {
            if (enemy == null)
            {
                continue;
            }

            enemies.Add(enemy);
            enemy.Died += HandleEnemyDied;
            enemy.SetFrozen(true);
            aliveEnemies++;
        }
    }

    private void BeginLevel()
    {
        if (currentLevelIndex < 0 || currentLevelIndex >= levels.Count)
        {
            return;
        }

        LevelDefinition level = levels[currentLevelIndex];
        if (dialogPrefab == null || level.openingDialog == null || level.openingDialog.Count == 0)
        {
            StartGameplay();
            return;
        }

        GameObject dialogInstance = Instantiate(dialogPrefab);
        DialogController dialog = dialogInstance.GetComponentInChildren<DialogController>(true);
        if (dialog == null)
        {
            Destroy(dialogInstance);
            StartGameplay();
            return;
        }

        dialog.Play(level.openingDialog, StartGameplay);
    }

    private void StartGameplay()
    {
        levelActive = true;
        PlayLevelMusic();

        PlayerMovement2D player = FindObjectOfType<PlayerMovement2D>();
        if (player != null)
        {
            player.SetFrozen(false);
        }

        StickController stick = FindObjectOfType<StickController>();
        if (stick != null)
        {
            stick.enabled = true;
        }

        foreach (EnemyController enemy in enemies)
        {
            if (enemy != null)
            {
                enemy.SetFrozen(false);
            }
        }
    }

    private void HandleEnemyDied(EnemyController enemy)
    {
        if (enemy != null)
        {
            enemy.Died -= HandleEnemyDied;
        }

        aliveEnemies = Mathf.Max(0, aliveEnemies - 1);
        if (aliveEnemies > 0 || levelCompleting || !levelActive)
        {
            return;
        }

        CompleteLevel();
    }

    private void CompleteLevel()
    {
        if (levelCompleting)
        {
            return;
        }

        levelCompleting = true;
        levelActive = false;
        StopLevelMusic();
        SetActorsFrozen(true);
        StartCoroutine(CompleteLevelRoutine());
    }

    private IEnumerator CompleteLevelRoutine()
    {
        if (completionDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(completionDelay);
        }

        int nextIndex = currentLevelIndex + 1;
        if (nextIndex < levels.Count)
        {
            StartLevel(nextIndex, true);
            yield break;
        }

        PlayTransition(endingSceneName, null);
    }

    public static void SetActorsFrozen(bool frozen)
    {
        PlayerMovement2D player = FindObjectOfType<PlayerMovement2D>();
        if (player != null)
        {
            player.SetFrozen(frozen);
        }

        StickController stick = FindObjectOfType<StickController>();
        if (stick != null)
        {
            stick.enabled = !frozen;
        }

        EnemyController[] found = FindObjectsOfType<EnemyController>();
        foreach (EnemyController enemy in found)
        {
            if (enemy != null)
            {
                enemy.SetFrozen(frozen);
            }
        }
    }

    private void PlayLevelMusic()
    {
        if (musicSource == null || levelMusic == null)
        {
            return;
        }

        musicSource.clip = levelMusic;
        musicSource.loop = true;
        musicSource.volume = musicVolume;

        if (!musicSource.isPlaying)
        {
            musicSource.Play();
        }
    }

    private void StopLevelMusic()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }

    private void PlayTransition(string sceneName, Action onComplete)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("LevelManager: target scene name is empty.");
            return;
        }

        ScreenFade fade = FindObjectOfType<ScreenFade>();
        if (fade == null)
        {
            GameObject prefab = Resources.Load<GameObject>("ScreenFadeBlack");
            if (prefab != null)
            {
                GameObject instanceObject = Instantiate(prefab);
                fade = instanceObject.GetComponent<ScreenFade>();
            }
        }

        if (fade != null)
        {
            DontDestroyOnLoad(fade.gameObject);
            fade.PlayTransition(sceneName, fadeInDuration, blackHoldDuration, fadeOutDuration, onComplete);
            return;
        }

        StartCoroutine(LoadSceneFallback(sceneName, onComplete));
    }

    private IEnumerator LoadSceneFallback(string sceneName, Action onComplete)
    {
        SceneManager.LoadScene(sceneName);
        yield return null;
        onComplete?.Invoke();
    }

    public static void RestartCurrentLevel()
    {
        if (UnityEngine.Object.FindObjectOfType<ScreenFade>() != null)
        {
            return;
        }

        if (instance != null && instance.levelCompleting)
        {
            return;
        }

        if (instance != null)
        {
            instance.ResetForRestart();
        }

        ScreenFade fade = null;
        GameObject prefab = Resources.Load<GameObject>("ScreenFade");
        if (prefab != null)
        {
            GameObject instanceObject = (GameObject)UnityEngine.Object.Instantiate(prefab);
            fade = instanceObject.GetComponent<ScreenFade>();
        }

        string sceneName = SceneManager.GetActiveScene().name;
        if (fade != null)
        {
            fade.FadeAndReload(sceneName);
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    private void ResetForRestart()
    {
        waitingForFade = false;
        levelActive = false;
        levelCompleting = false;
        skipOpeningDialog = true;
        aliveEnemies = 0;
        enemies.Clear();

        if (currentLevelIndex >= 0 && currentLevelIndex < levels.Count)
        {
            pendingLevelIndex = currentLevelIndex;
        }
        else
        {
            pendingLevelIndex = -1;
        }
    }
}
