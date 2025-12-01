using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

public class GameManager : MonoBehaviour
{
    // ===================================
    // 1. 변수 선언 및 참조
    // ===================================
    [Header("오디오 및 월드 오브젝트")]
    [SerializeField] private AudioSource bgm;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private GameObject startWorld;
    [SerializeField] private GameObject btn;

    [Header("관리 스크립트")]
    public FadeScript fadeScript;
    public ButtonManager buttonManager;
    public PhotoSwitcher PhotoSwitcher;

    [Header("플레이어 및 UI 참조")]
    public GameObject playerGameObject;
    public PlayerController playerController;
    public Slider hpSlider;

    [Header("HUD Texts - 개별 제어")]
    public Text scoreText;
    public Text hpText;
    public Text gameOverText;

    [Header("UI Links")]
    [Tooltip("element7 버튼(GoTela_2)을 인스펙터에 드래그해서 할당하세요.")]
    [SerializeField] private GameObject goTela2;

    [Header("HUD")]
    [Tooltip("항상 켜져 있어야 할 HUD Canvas (Panel)")]
    [SerializeField] private GameObject hudCanvas;

    [Header("전투 소환 설정")]
    [SerializeField] private GameObject bulletSpawnerPrefab;
    [SerializeField] private GameObject bowPrefab;
    [SerializeField] private Transform bowParent;
    [SerializeField] private AudioClip[] bgmClips;
    [SerializeField] private bool destroyPreviousOnSpawn = true;
    [SerializeField] private GameObject bossSpawnVfxPrefab;

    [Header("Victory")]
    [SerializeField] private AudioClip victoryClip;
    [Range(0f, 1f)][SerializeField] private float victoryVolume = 1f;
    [SerializeField] private float victoryDelay = 2f;

    [Header("Victory UI")]
    [SerializeField] private GameObject winTextObject;
    [SerializeField] private GameObject endTextObject; // ⬅️ 최종 승리(스코어 5) 텍스트

    // --- 런타임 참조 변수 ---
    private readonly List<BulletSpawner> activeBosses = new List<BulletSpawner>();
    private List<BossSpawnInfo> pendingBossSpawns = new List<BossSpawnInfo>();
    private int teleportSpawnIndex = 0;
    private GameObject currentBow;
    private readonly List<GameObject> spawnedBows = new List<GameObject>();
    private int lastCombatIndex = -1;
    private bool suppressBgmChanges = false;
    public event Action ParkChangeEvent;
    private static GameManager instance;
    public bool Isworld_1 { get; private set; }
    private bool isGameOver = false;
    private int currentScore = 0; // ⬅️ 현재 스코어 변수
    private bool isGameActive = false; // ⬅️ 게임 활성화 상태 플래그

    // --- 확장성을 위한 새 클래스 정의 ---
    [System.Serializable]
    public enum BossSpawnTrigger
    {
        OnStart,
        OnBossHealthThreshold
    }

    [System.Serializable]
    public class BossSpawnInfo
    {
        public GameObject bossPrefab;
        public Transform spawnPoint;
        public BossSpawnTrigger spawnTrigger = BossSpawnTrigger.OnStart;
        public float triggerValue = 0.4f;
    }

    // ===================================
    // CombatSpawnSetting 클래스
    // ===================================
    [System.Serializable]
    public class CombatSpawnSetting
    {
        public string name;
        public bool enable = false;
        public AudioClip bgmClip;
        [Header("소환할 보스 목록")]
        public List<BossSpawnInfo> bossesToSpawn = new List<BossSpawnInfo>();
        [Header("무기 설정")]
        public GameObject bowPrefabOverride;
        public Transform bowSpawnOverride;
    }

    [Header("Combat Settings per Photo Index")]
    public CombatSpawnSetting[] combatSettings;

    // ===================================
    // 2. 싱글톤 및 초기화
    // ===================================
    #region Singleton
    private void Awake()
    {
        if (instance == null) { instance = this; }
        else if (instance != this) { Destroy(gameObject); return; }

        if (sfxSource == null)
        {
            sfxSource = gameObject.GetComponent<AudioSource>();
            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
        }
        if (bgm != null)
        {
            bgm.playOnAwake = false;
            bgm.loop = true;
            bgm.spatialBlend = 0f;
        }
    }

    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<GameManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("GameManager");
                    instance = go.AddComponent<GameManager>();
                }
            }
            return instance;
        }
    }
    #endregion

    // ===================================
    // 3. Unity Lifecycle
    // ===================================
    void Start()
    {
        Isworld_1 = true;
        if (playerGameObject != null)
            playerGameObject.transform.rotation = Quaternion.identity;
        if (hudCanvas != null)
            hudCanvas.SetActive(true);
        if (playerController != null)
            playerController.OnHealthChanged += UpdateHealthUI;
        isGameActive = false;
    }

    void Update()
    {
        if (isGameActive && !isGameOver && !suppressBgmChanges)
        {
            if (hudCanvas != null && !hudCanvas.activeSelf)
                hudCanvas.SetActive(true);

            if (scoreText != null && !scoreText.gameObject.activeSelf)
                scoreText.gameObject.SetActive(true);

            if (hpText != null && !hpText.gameObject.activeSelf)
                hpText.gameObject.SetActive(true);

            if (hpSlider != null && !hpSlider.gameObject.activeSelf)
                hpSlider.gameObject.SetActive(true);
        }
    }

    void OnEnable()
    {
        if (PhotoSwitcher != null)
        {
            PhotoSwitcher.OnPhotoChanged += HandlePhotoChangedDuringBlack;
            PhotoSwitcher.OnPhotoChangeCompleted += HandlePhotoChangeCompleted;
        }
    }
    void OnDisable()
    {
        if (PhotoSwitcher != null)
        {
            PhotoSwitcher.OnPhotoChanged -= HandlePhotoChangedDuringBlack;
            PhotoSwitcher.OnPhotoChangeCompleted -= HandlePhotoChangeCompleted;
        }
    }
    void OnDestroy()
    {
        if (playerController != null)
            playerController.OnHealthChanged -= UpdateHealthUI;
        ClearActiveBosses();
    }

    private void ShowCombatUI()
    {
        scoreText?.gameObject.SetActive(true);
        hpText?.gameObject.SetActive(true);
        gameOverText?.gameObject.SetActive(false);
        hpSlider?.gameObject.SetActive(true);
    }
    private void ShowGameOverUI()
    {
        scoreText?.gameObject.SetActive(false);
        hpText?.gameObject.SetActive(false);
        gameOverText?.gameObject.SetActive(true);
        hpSlider?.gameObject.SetActive(true);
    }
    private void HideAllHudTexts()
    {
        scoreText?.gameObject.SetActive(false);
        hpText?.gameObject.SetActive(false);
        gameOverText?.gameObject.SetActive(false);
        hpSlider?.gameObject.SetActive(false);
        Debug.Log("[HUD] 모든 HUD 숨김 (승리)");
    }

    // ===================================
    // 5. HP UI 및 GameOver
    // ===================================
    private void UpdateHealthUI(float ratio, float currentHp)
    {
        if (hpSlider != null)
        {
            hpSlider.value = ratio;
        }
        if (hpText != null)
        {
            hpText.text = currentHp.ToString("F0");
        }

        if (currentHp <= 0f && !isGameOver)
        {
            isGameOver = true;
            isGameActive = false;
            ShowGameOverUI();
            StartCoroutine(GameOverCoroutine());
        }
    }
    private IEnumerator GameOverCoroutine()
    {
        yield return new WaitForSeconds(1.0f);
        StartTransition(RestartGame);
    }

    // ===================================
    // 6. 페이드 및 전환
    // ===================================
    public void StartTransition(Action nextAction) => StartTransition(nextAction, null);
    public void StartTransition(Action nextAction, Action onComplete) => StartCoroutine(FadeAndNext(nextAction, onComplete));
    // ⬇️ [!!! 이 함수가 모든 문제의 원인입니다. 이걸로 덮어쓰세요 !!!] ⬇️
    // ⬇️ [!!! 2. 이 함수를 덮어쓰세요 (멈춤 문제 해결) !!!] ⬇️
    private IEnumerator FadeAndNext(Action nextAction, Action onComplete)
    {
        // [수정] Time.timeScale이 0이어도 멈추지 않도록
        // 모든 대기를 '현실 시간' (Realtime) 기준으로 변경합니다.

        float fadeOutTime = 0.5f; // 페이드 아웃에 걸릴 현실 시간
        float fadeInTime = 0.2f;  // 페이드 인에 걸릴 현실 시간

        // FadeScript가 오류를 일으킬 경우를 대비해 try-catch로 감쌉니다.
        try
        {
            if (fadeScript != null)
            {
                StartCoroutine(fadeScript.FadeOutCoroutine());
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"FadeOutCoroutine 실행 중 오류 발생: {e.Message}");
        }

        // FadeScript가 멈추든 말든, 0.5초(현실 시간)를 기다립니다.
        yield return new WaitForSecondsRealtime(fadeOutTime);


        nextAction?.Invoke(); // 씬 또는 포토 변경 (element 5로 이동)


        try
        {
            if (fadeScript != null)
            {
                StartCoroutine(fadeScript.FadeInCoroutine());
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"FadeInCoroutine 실행 중 오류 발생: {e.Message}");
        }

        // FadeScript가 멈추든 말든, 0.2초(현실 시간)를 기다립니다.
        yield return new WaitForSecondsRealtime(fadeInTime);


        // [중요] onComplete (노래 바꾸기, '+' 버튼 켜기)가 이제 반드시 실행됩니다.
        onComplete?.Invoke();
    }

    public void OnStartButtonClicked() => StartTransition(LoadEntry);
    public void OnWorldChangeButtonClicked() => StartTransition(WorldChange);
    public void WorldChange() => ParkChangeEvent?.Invoke();
    public void RestartGame() => SceneManager.LoadScene("SampleScene");

    private void LoadEntry()
    {
        if (bgm != null && !bgm.isPlaying) bgm.Play();
        startWorld.SetActive(false);
        buttonManager.gameObject.SetActive(false);
        btn.SetActive(true);

        if (PhotoSwitcher != null)
        {
            PhotoSwitcher.ShowHiddenButton();
        }

        ShowCombatUI();
        isGameActive = true;
    }

    // ===================================
    // 7. PhotoSwitcher 이벤트 [수정됨]
    // ===================================
    private void HandlePhotoChangedDuringBlack(int index)
    {
        if (index == 6 && lastCombatIndex == 6)
        {
            Debug.Log("[승리 후] 인덱스 6 자동 소환 차단!");
            return;
        }

        var containers = PhotoSwitcher.photoContainers;
        if (containers == null || index < 0 || index >= containers.Length) return;

        CombatSpawnSetting setting = null;
        if (combatSettings != null && index < combatSettings.Length)
            setting = combatSettings[index];

        if (setting != null && setting.enable)
        {
            Debug.Log($"HandlePhotoChangedDuringBlack: index {index}은 [전투 지역]입니다. 보스를 소환합니다.");
            SpawnCombatAtIndex(index, containers[index]?.transform);
        }
        else
        {
            Debug.Log($"HandlePhotoChangedDuringBlack: index {index}은 [비전투 지역]입니다. 보스/활을 정리합니다.");

            if (destroyPreviousOnSpawn)
            {
                ClearActiveBosses();
                DestroyAllSpawnedBows();
                if (currentBow != null) { Destroy(currentBow); currentBow = null; }
            }

            if (!suppressBgmChanges && bgm != null)
            {
                AudioClip safeNormalClip = GetSafeNormalBGM();
                if (bgm.clip != safeNormalClip && safeNormalClip != null)
                {
                    bgm.Stop();
                    bgm.clip = safeNormalClip;
                    bgm.loop = true;
                    bgm.Play();
                    Debug.Log($"[비전투 지역] 기본 BGM 재생: {safeNormalClip.name}");
                }
            }
        }
    }

    private void HandlePhotoChangeCompleted(int index) { }


    // ===================================
    // 8. 보스 처치 시퀀스
    // ===================================
    // ⬇️ [!!! 1. 이 함수를 덮어쓰세요 (스코어 포맷 수정, 힐 로직 제거) !!!] ⬇️
    private void HandleSpawnerDead(BulletSpawner spawner)
    {
        if (spawner == null) return;
        spawner.OnDead -= HandleSpawnerDead;
        spawner.OnThresholdReached -= HandleBossThresholdReached;

        if (activeBosses.Contains(spawner))
        {
            activeBosses.Remove(spawner);
        }

        // --- [!!! 1. 스코어 로직 수정 !!!] ---
        currentScore++; // ⬅️ 스코어 1 증가
        if (scoreText != null)
        {
            // [수정] "Score : " 텍스트를 추가합니다.
            scoreText.text = "Score : " + currentScore.ToString();
        }
        // --- [!!! 1. 수정 끝 !!!] ---

        Debug.Log($"보스 1명 처치! 남은 활성 보스: {activeBosses.Count}명. 남은 대기 보스: {pendingBossSpawns.Count}명. 현재 스코어: {currentScore}");

        if (activeBosses.Count == 0 && pendingBossSpawns.Count == 0)
        {
            Debug.Log("모든 보스 처치! 승리 시퀀스를 시작합니다.");
            StartCoroutine(SpawnerDeadSequence(currentScore));
        }
    }

    private void HandleBossThresholdReached(BulletSpawner triggeredBoss)
    {
        Debug.Log($"보스 HP 트리거 발동! ({triggeredBoss.name})");
        triggeredBoss.OnThresholdReached -= HandleBossThresholdReached;

        if (pendingBossSpawns.Count > 0)
        {
            var nextBossInfo = pendingBossSpawns[0];
            if (nextBossInfo.spawnTrigger == BossSpawnTrigger.OnBossHealthThreshold)
            {
                Debug.Log($"다음 보스 소환: {nextBossInfo.bossPrefab.name}");
                pendingBossSpawns.RemoveAt(0);
                Transform defaultParent = (PhotoSwitcher.photoContainers != null && lastCombatIndex >= 0 && lastCombatIndex < PhotoSwitcher.photoContainers.Length)
                    ? PhotoSwitcher.photoContainers[lastCombatIndex].transform : null;
                SpawnBoss(nextBossInfo, defaultParent);
            }
        }
    }
    // ⬇️ [!!! 이 함수 하나만 덮어쓰세요 !!!] ⬇️
    // ⬇️ [!!! 이 함수를 통째로 덮어쓰세요 !!!] ⬇️
    // ⬇️ [!!! 2. 이 함수를 덮어쓰세요 (힐 로직 추가) !!!] ⬇️
    private IEnumerator SpawnerDeadSequence(int scoreAtWin) // ⬅️ 스코어를 받음
    {
        isGameActive = false;
        HideAllHudTexts();

        // --- 1. 승리 텍스트 분기 ---
        if (scoreAtWin >= 5)
        {
            endTextObject?.SetActive(true);
        }
        else
        {
            winTextObject?.SetActive(true);
        }
        // --- 1. 수정 끝 ---

        DestroyAllSpawnedBows();
        if (currentBow != null) { Destroy(currentBow); currentBow = null; }
        if (bgm != null && bgm.isPlaying) bgm.Stop();

        float waitTime = victoryDelay;
        if (victoryClip != null)
        {
            if (sfxSource != null) sfxSource.PlayOneShot(victoryClip, Mathf.Clamp01(victoryVolume));
            else AudioSource.PlayClipAtPoint(victoryClip, Camera.main?.transform.position ?? transform.position, Mathf.Clamp01(victoryVolume));
            waitTime = Mathf.Max(victoryDelay, victoryClip.length);
        }
        suppressBgmChanges = true;

        // 현실 시간만큼 자동으로 기다립니다.
        yield return new WaitForSecondsRealtime(waitTime);

        winTextObject?.SetActive(false);
        endTextObject?.SetActive(false);

        // --- 승리 후 이동 분기 로직 ---
        int targetIndex = -1;
        int defeatedIndex = lastCombatIndex;
        GameObject buttonToDisable = null;

        if (defeatedIndex == 7)
        {
            targetIndex = 6;
            buttonToDisable = goTela2;
        }
        else if (defeatedIndex == 9)
        {
            targetIndex = 8;
            // buttonToDisable = goTela9_Button;
        }
        else
        {
            Debug.LogWarning($"알 수 없는 보스(Index: {defeatedIndex}) 승리! 기본 6번으로 이동합니다.");
            targetIndex = 6;
        }

        Action action = () =>
        {
            if (PhotoSwitcher != null)
            {
                PhotoSwitcher.SetPhotoImmediate(targetIndex);
                var containers = PhotoSwitcher.photoContainers;
                if (containers != null)
                {
                    for (int i = 0; i < containers.Length; i++)
                        if (containers[i] != null) containers[i].SetActive(i == targetIndex);

                    if (defeatedIndex >= 0 && defeatedIndex < containers.Length && containers[defeatedIndex] != null)
                        containers[defeatedIndex].SetActive(false);
                }
            }
            if (buttonToDisable != null)
                buttonToDisable.SetActive(false);

            lastCombatIndex = targetIndex;
        };

        // --- [!!! 3. UI 및 상태 복구 로직 (힐 추가) !!!] ---
        Action onComplete = () =>
        {
            // --- [!!! 힐 로직을 여기로 이동 !!!] ---
            // (보상으로 회복 50)
            if (playerController != null)
            {
                Debug.Log("전투 승리 보상! 플레이어 HP 50 회복.");
                playerController.Heal(50f);
            }
            // --- [!!! 이동 끝 !!!] ---

            suppressBgmChanges = false;
            AudioClip safeNormalClip = GetSafeNormalBGM();
            if (bgm != null && safeNormalClip != null)
            {
                bgm.Stop();
                bgm.clip = safeNormalClip;
                bgm.loop = true;
                bgm.Play();
            }

            if (combatSettings != null && defeatedIndex >= 0 && defeatedIndex < combatSettings.Length)
            {
                combatSettings[defeatedIndex].enable = false;
            }

            if (btn != null)
            {
                btn.SetActive(true);
            }

            isGameActive = true;
        };
        // --- [!!! 3. 수정 끝 !!!] ---

        Time.timeScale = 1f;
        StartTransition(action, onComplete);
    }

    // ===================================
    // 9. BGM 안전 선택
    // ===================================
    private AudioClip GetSafeNormalBGM()
    {
        if (bgmClips == null || bgmClips.Length == 0) return null;
        if (bgmClips[0] != null && !bgmClips[0].name.ToLower().Contains("boss"))
            return bgmClips[0];
        for (int i = 0; i < bgmClips.Length; i++)
        {
            if (bgmClips[i] != null && !bgmClips[i].name.ToLower().Contains("boss"))
                return bgmClips[i];
        }
        Debug.LogWarning("[GetSafeNormalBGM] Boss BGM만 있음. 첫 번째 반환.");
        return bgmClips[0];
    }

    // ===================================
    // 10. 전투 소환
    // ===================================
    // ⬇️ [!!! 이 함수 전체를 덮어쓰세요 !!!] ⬇️
    public void SpawnCombatAtIndex(int index, Transform containerTransform)
    {
        if (PhotoSwitcher == null || index < 0) return;
        if (index == 6 && lastCombatIndex == 6) { return; }

        CombatSpawnSetting setting = null;
        if (combatSettings != null && index < combatSettings.Length)
            setting = combatSettings[index];

        if (setting == null || !setting.enable || setting.bossesToSpawn == null || setting.bossesToSpawn.Count == 0)
        {
            Debug.Log($"SpawnCombatAtIndex: index {index} has no active combat or bosses. skipping.");
            return;
        }

        if (PhotoSwitcher.photoContainers != null)
            foreach (var c in PhotoSwitcher.photoContainers)
                if (c != null) c.SetActive(false);

        if (destroyPreviousOnSpawn)
        {
            ClearActiveBosses();
            DestroyAllSpawnedBows();
            if (currentBow != null) { Destroy(currentBow); currentBow = null; }
        }

        // --- [!!! 핵심 수정 로직 시작 !!!] ---

        // 1. 스폰 목록을 초기화합니다.
        pendingBossSpawns.Clear();
        teleportSpawnIndex = 0;

        // 2. 'OnStart' 보스와 'OnHealthThreshold' 보스를 미리 분리합니다.
        List<BossSpawnInfo> onStartSpawns = new List<BossSpawnInfo>();

        foreach (var bossInfo in setting.bossesToSpawn)
        {
            if (bossInfo.spawnTrigger == BossSpawnTrigger.OnStart)
            {
                onStartSpawns.Add(bossInfo);
            }
            else if (bossInfo.spawnTrigger == BossSpawnTrigger.OnBossHealthThreshold)
            {
                // 'GameManager'의 'pendingBossSpawns'에 후속 보스만 추가합니다.
                pendingBossSpawns.Add(bossInfo);
            }
        }

        // 3. 'OnStart' 보스들을 먼저 스폰합니다.
        //    (이제 pendingBossSpawns는 '보스 2' 정보만 갖고 있습니다.)
        foreach (var bossToSpawn in onStartSpawns)
        {
            SpawnBoss(bossToSpawn, containerTransform); // ⬅️ 보스 1 스폰
        }
        // --- [!!! 핵심 수정 로직 끝 !!!] ---


        // 4. 활과 BGM을 설정합니다. (기존 로직)
        GameObject bowSource = setting.bowPrefabOverride ?? bowPrefab;
        if (bowSource != null)
            SpawnBowInstance(setting, containerTransform, bowSource);

        AudioClip clipToPlay = setting.bgmClip ?? (bgmClips != null && index < bgmClips.Length ? bgmClips[index] : null);
        if (!suppressBgmChanges && bgm != null && clipToPlay != null)
        {
            bgm.clip = clipToPlay;
            bgm.loop = true;
            bgm.Play();
        }
        lastCombatIndex = index;
    }

    private BulletSpawner SpawnBoss(BossSpawnInfo info, Transform defaultParent)
    {
        if (info.bossPrefab == null)
        {
            Debug.LogError("BossSpawnInfo에 프리팹이 없습니다!");
            return null;
        }

        Transform spawnParent = info.spawnPoint ?? defaultParent;
        Vector3 spawnPos = (spawnParent != null) ? spawnParent.position : Vector3.zero;
        Quaternion spawnRot = (spawnParent != null) ? spawnParent.rotation : Quaternion.identity;

        // 1. 보스 생성
        GameObject bossGO = Instantiate(info.bossPrefab, spawnPos, spawnRot);

        // 2. [!!! 파티클 수정 !!!]
        // (Play On Awake가 꺼져있으므로, 생성 후 직접 Play()를 호출합니다)
        if (bossSpawnVfxPrefab != null)
        {
            GameObject vfxGO = Instantiate(bossSpawnVfxPrefab, spawnPos, spawnRot);
            ParticleSystem ps = vfxGO.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Play(); // ⬅️ 강제 재생!
            }
            else
            {
                Debug.LogWarning("Boss Spawn Vfx Prefab에 ParticleSystem 컴포넌트가 없습니다!");
            }
        }

        BulletSpawner spawnerScript = bossGO.GetComponent<BulletSpawner>();
        if (spawnerScript == null)
        {
            Debug.LogError($"[{info.bossPrefab.name}]에서 BulletSpawner.cs를 찾을 수 없습니다!");
            Destroy(bossGO);
            return null;
        }

        // 3. 이벤트 연결
        spawnerScript.OnDead += HandleSpawnerDead;
        spawnerScript.InitializeTeleport(teleportSpawnIndex);
        teleportSpawnIndex++;
        activeBosses.Add(spawnerScript);

        // 4. [!!! 여기가 진짜 핵심 !!!]
        //    (이제 pendingBossSpawns에는 '보스 2'만 있으므로 이 로직이 성공합니다)
        if (pendingBossSpawns.Count > 0 && pendingBossSpawns[0].spawnTrigger == BossSpawnTrigger.OnBossHealthThreshold)
        {
            Debug.Log($"[{spawnerScript.name}]에게 다음 보스 소환 트리거(HP)를 설정합니다.");

            // [핵심 1] 보스가 HP 40% 됐다고 외치면, GameManager가 듣도록 '구독'
            spawnerScript.OnThresholdReached += HandleBossThresholdReached;

            // [핵심 2] 그 HP 40%라는 '기준값'을 보스에게 전달
            float threshold = pendingBossSpawns[0].triggerValue;
            spawnerScript.SetHealthThreshold(threshold);
        }

        return spawnerScript;
    }

    // ===================================
    // 11. 활 소환 + XR 설정
    // ===================================
    private void SpawnBowInstance(CombatSpawnSetting setting, Transform containerTransform, GameObject bowSource)
    {
        Transform bowSpawnParent = setting?.bowSpawnOverride ?? bowParent ?? containerTransform;
        if (bowSpawnParent == null) { Debug.LogError("SpawnBowInstance: 부모 없음!"); return; }

        GameObject newBow;
        if (bowSpawnParent == bowParent)
        {
            newBow = Instantiate(bowSource, bowSpawnParent);
            newBow.transform.localPosition = Vector3.zero;
            newBow.transform.localRotation = Quaternion.identity;
        }
        else
        {
            newBow = Instantiate(bowSource, bowSpawnParent.position, bowSpawnParent.rotation);
        }

        currentBow = newBow;
        spawnedBows.Add(newBow);
        EnsureBowHasPhysicsAndInteractable(newBow);
    }

    private void EnsureBowHasPhysicsAndInteractable(GameObject bow)
    {
        if (bow == null) return;
        Collider col = bow.GetComponentInChildren<Collider>();
        if (col == null) { col = bow.AddComponent<CapsuleCollider>(); }
        col.isTrigger = false;

        Rigidbody rb = bow.GetComponent<Rigidbody>();
        if (rb == null) rb = bow.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        XRGrabInteractable grab = bow.GetComponent<XRGrabInteractable>();
        if (grab == null) grab = bow.AddComponent<XRGrabInteractable>();

        if (grab.attachTransform == null)
        {
            Transform attach = bow.transform.Find("AttachPoint");
            if (attach == null)
            {
                GameObject at = new GameObject("AttachPoint");
                at.transform.SetParent(bow.transform, false);
                at.transform.localPosition = Vector3.zero;
                attach = at.transform;
            }
            grab.attachTransform = attach;
        }

        BowGrabSetup helper = bow.GetComponent<BowGrabSetup>();
        if (helper == null) helper = bow.AddComponent<BowGrabSetup>();
        helper.Initialize(rb, grab);
    }


    // ===================================
    // 12. 유틸리티
    // ===================================
    private void DestroyAllSpawnedBows()
    {
        for (int i = spawnedBows.Count - 1; i >= 0; i--)
        {
            if (spawnedBows[i] != null) Destroy(spawnedBows[i]);
        }
        spawnedBows.Clear();
    }

    private void ClearActiveBosses()
    {
        for (int i = activeBosses.Count - 1; i >= 0; i--)
        {
            var boss = activeBosses[i];
            if (boss != null)
            {
                boss.OnDead -= HandleSpawnerDead;
                boss.OnThresholdReached -= HandleBossThresholdReached;
                Destroy(boss.gameObject);
            }
        }
        activeBosses.Clear();
    }


    public void RespawnAtLastCombat()
    {
        if (lastCombatIndex < 0 || PhotoSwitcher == null) return;
        var containers = PhotoSwitcher.photoContainers;
        Transform container = (containers != null && lastCombatIndex < containers.Length) ? containers[lastCombatIndex].transform : null;
        if (container != null)
        {
            SpawnCombatAtIndex(lastCombatIndex, container);
            ShowCombatUI();
            isGameOver = false;
            isGameActive = true;
        }
    }
}