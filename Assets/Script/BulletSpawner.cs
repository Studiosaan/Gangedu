using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletSpawner : MonoBehaviour, IHittable
{
    [Header("Bullet Settings")]
    public GameObject bulletPrefab;
    [SerializeField] public float bulletSpeed = 30f;

    [Header("VFX (Child)")]
    [Tooltip("보스의 자식 - 나타날 때 파티클")]
    [SerializeField] private ParticleSystem appearVfx;
    [Tooltip("보스의 자식 - 사라질 때 파티클")]
    [SerializeField] private ParticleSystem disappearVfx;
    [SerializeField] private float teleportVfxDelay = 0.5f; // 이펙트 재생을 위한 대기 시간

    [Header("Spawner Settings")]
    public Transform firePoint;
    public float spawnRateMin = 0.5f;
    public float spawnRateMax = 3f;
    public int maxBullets = 5;

    [Header("Audio & Health")]
    public float hp = 100.0f;
    public AudioClip fireClip;
    public ParticleSystem deadEffect;

    [Tooltip("사망(터짐) 시 재생할 오디오 클립")]
    public AudioClip deathClip;
    [Tooltip("사운드 볼륨")]
    public float deathClipVolume = 5f;

    // 사망시 외부 통보 이벤트 (GameManager가 구독)
    public event Action<BulletSpawner> OnDead;

    // ⬇️ [추가] HP가 일정 수준 이하일 때 발생하는 이벤트
    public event Action<BulletSpawner> OnThresholdReached;
    private bool hasTriggeredThreshold = false; // 이벤트 중복 방지 플래그
    private float maxHp; // ⬇️ [추가] 최대 HP를 저장할 변수

    // ========== 수직 조준 관련 설정 ==========
    public enum AimMode { HeadOnly, TorsoOnly, Random, Alternate }
    [Header("Aim (Vertical) Settings)")]
    public AimMode aimMode = AimMode.Random;
    public Transform headTarget;
    public Transform torsoTarget;
    public float headYOffset = 0f;
    public float torsoYOffset = -0.5f;

    // ========== ⬇️ [수정] 순간이동 설정 ==========
    [Header("순간이동 설정")]
    [Tooltip("보스가 순간이동할 위치(Transform) 목록")]
    [SerializeField] private Transform[] teleportPoints;
    [Tooltip("순간이동 주기 (초)")]
    [SerializeField] private float teleportInterval = 5.0f;
    private float teleportTimer;
    private int currentPointIndex = -1;
    private bool isTeleporting = false; // 순간이동 중복 실행 방지 플래그
    // ==========================================

    private AudioSource fireAudio;
    private Transform cameraTarget;
    private float spawnRate;
    private float timeAfterSpawn;

    private readonly List<GameObject> spawnedBullets = new List<GameObject>();
    private bool alternateToggle = false;
    private readonly List<StickingArrowToSurface> stuckArrows = new List<StickingArrowToSurface>();

    void Start()
    {
        timeAfterSpawn = 0f;
        spawnRate = UnityEngine.Random.Range(spawnRateMin, spawnRateMax);
        cameraTarget = FindObjectOfType<Camera>()?.transform;
        fireAudio = GetComponent<AudioSource>();
        if (fireAudio == null)
            fireAudio = gameObject.AddComponent<AudioSource>();

        if (firePoint == null)
        {
            firePoint = this.transform;
        }

        // ⬇️ [수정] 순간이동 타이머 및 최대 HP 설정
        maxHp = hp; // ⬅️ 최대 HP 기록
        teleportTimer = teleportInterval;
    }

    /// <summary>
    /// 순간이동 시작 위치 인덱스를 설정하고 즉시 그 위치로 이동시킵니다.
    /// </summary>
    public void InitializeTeleport(int startIndex)
    {
        if (teleportPoints == null || teleportPoints.Length == 0) return;

        currentPointIndex = startIndex % teleportPoints.Length;
        Transform targetPoint = teleportPoints[currentPointIndex];
        if (targetPoint != null)
        {
            transform.position = targetPoint.position;
            Debug.Log($"{name} 순간이동 시작 위치 설정: {targetPoint.name} (인덱스 {currentPointIndex})");
        }
        teleportTimer = teleportInterval;
    }

    void Update()
    {
        if (cameraTarget == null) return;

        // ⬇️ [수정] 순간이동 로직 (타이머 수정됨)
        HandleTeleportation();

        // --- 기존 조준 로직 ---
        bool aimHead = false;
        switch (aimMode)
        {
            case AimMode.HeadOnly: aimHead = true; break;
            case AimMode.TorsoOnly: aimHead = false; break;
            case AimMode.Random: aimHead = (UnityEngine.Random.value > 0.5f); break;
            case AimMode.Alternate: alternateToggle = !alternateToggle; aimHead = alternateToggle; break;
        }

        Vector3 targetPos;
        if (aimHead)
            targetPos = headTarget != null ? headTarget.position : cameraTarget.position + Vector3.up * headYOffset;
        else
            targetPos = torsoTarget != null ? torsoTarget.position : cameraTarget.position + Vector3.up * torsoYOffset;

        Vector3 lookDirection = targetPos - transform.position;
        if (lookDirection.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(-lookDirection);
        }

        // --- 기존 총알 발사 로직 ---
        timeAfterSpawn += Time.deltaTime;
        if (timeAfterSpawn >= spawnRate)
        {
            timeAfterSpawn = 0f;
            Vector3 spawnPosition = (firePoint != null) ? firePoint.position : transform.position;
            Vector3 aimDirection = (targetPos - spawnPosition).normalized;
            if (aimDirection.sqrMagnitude <= 0.0001f) aimDirection = transform.forward;
            Quaternion spawnRotation = Quaternion.LookRotation(aimDirection);
            GameObject bullet = Instantiate(bulletPrefab, spawnPosition, spawnRotation);
            Bullet bulletScript = bullet.GetComponent<Bullet>();
            if (bulletScript != null)
                bulletScript.SetupBullet(bulletSpeed);
            bullet.SetActive(true);
            spawnedBullets.Add(bullet);
            if (spawnedBullets.Count > maxBullets)
            {
                Destroy(spawnedBullets[0]);
                spawnedBullets.RemoveAt(0);
            }
            if (fireAudio != null && fireClip != null)
                fireAudio.PlayOneShot(fireClip);
            spawnRate = UnityEngine.Random.Range(spawnRateMin, spawnRateMax);
        }
    }

    // ⬇️ [수정] 순간이동 함수 (타이머 로직 수정됨)
    private void HandleTeleportation()
    {
        if (teleportPoints == null || teleportPoints.Length == 0) return;

        // 텔레포트 중일 때는 타이머가 멈춰야 하므로 리턴
        if (isTeleporting)
        {
            return;
        }

        // 텔레포트 중이 아닐 때만 타이머 감소
        teleportTimer -= Time.deltaTime;

        if (teleportTimer <= 0f)
        {
            StartCoroutine(DoTeleportSequence());
        }
    }

    // ⬇️ [수정] 순간이동 시퀀스 (자식 파티클 재생 방식)
    private IEnumerator DoTeleportSequence()
    {
        // 1. 순간이동 시작
        isTeleporting = true;

        // --- 2. 사라지기 ---
        if (disappearVfx != null)
        {
            disappearVfx.Play(); // ⬅️ "사라짐" 이펙트 재생
        }

        // --- [!!! 핵심 수정 !!!] ---
        // 보스 모델 숨기기 (단, 파티클 렌더러는 숨기지 않음)
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            // 'ParticleSystemRenderer'가 아니면 (즉, 보스 모델이면) 숨긴다
            if (!(r is ParticleSystemRenderer))
            {
                r.enabled = false;
            }
        }
        // 콜라이더는 파티클과 상관 없으므로 모두 숨김
        foreach (Collider c in GetComponentsInChildren<Collider>())
            c.enabled = false;
        // --- [!!! 수정 끝 !!!] ---


        // --- 3. 이펙트 재생 대기 ---
        yield return new WaitForSeconds(teleportVfxDelay); // 인스펙터에서 설정한 1초 대기

        // --- 4. 실제 위치 이동 ---
        currentPointIndex = (currentPointIndex + 1) % teleportPoints.Length;
        Transform targetPoint = teleportPoints[currentPointIndex];

        if (targetPoint != null)
        {
            transform.position = targetPoint.position;
        }
        else
        {
            Debug.LogWarning($"순간이동 지점 {currentPointIndex}가 비어있습니다.");
        }

        // --- 5. 나타나기 (이펙트 먼저) ---
        if (appearVfx != null)
        {
            appearVfx.Play(); // ⬅️ "나타남" 이펙트 재생
        }

        // 이펙트가 보일 잠깐의 시간
        yield return new WaitForSeconds(0.1f);

        // --- 6. 보스 모델 보이기 ---
        // 모든 렌더러를 다시 켠다 (파티클 포함, 어차피 켜져있었음)
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
            r.enabled = true;
        foreach (Collider c in GetComponentsInChildren<Collider>())
            c.enabled = true;

        // --- 7. 순간이동 완료 ---
        isTeleporting = false;
        teleportTimer = teleportInterval; // 텔레포트가 '끝난 후' 타이머 리셋
    }

    // IHittable 인터페이스 구현
    public void GetHit() => ApplyDamage(10f);

    // 외부 데미지 적용
    public void GetDamage(float amount) => ApplyDamage(amount);

    // ⬇️ [!!! 핵심 수정 !!!] 데미지 처리 및 HP 임계값 체크
    // ⬇️ [!!! 핵심 수정 !!!] 데미지 처리 및 HP 임계값 체크
    private void ApplyDamage(float amount)
    {
        if (hp <= 0f) return; // 이미 죽음

        hp -= amount;

        if (healthThreshold > 0 && !hasTriggeredThreshold)
        {
            float currentHpRatio = hp / maxHp;
            if (currentHpRatio <= healthThreshold)
            {
                hasTriggeredThreshold = true;
                OnThresholdReached?.Invoke(this);
            }
        }

        // 사망 처리
        if (hp <= 0f)
        {
            DetachAllStuckArrows();
            if (deadEffect != null)
            {
                ParticleSystem ps = Instantiate(deadEffect, transform.position, transform.rotation);
                ps.Play();
                Destroy(ps.gameObject, ps.main.duration + ps.main.startLifetime.constantMax);
            }

            if (deathClip != null)
            {
                // [수정됨] 3번 방법: 커스텀 3D 사운드
                // 보스 위치에 소리를 만들되, "최소 거리(MinDistance)"를 크게 잡아서
                // 멀리 있어도 소리가 작아지지 않게 설정합니다.

                GameObject audioObj = new GameObject("BossDeathSound_3D_Loud");
                audioObj.transform.position = transform.position; // ⬅️ 보스 위치에서 소리 남 (방향감 유지)

                AudioSource source = audioObj.AddComponent<AudioSource>();
                source.clip = deathClip;
                source.volume = deathClipVolume;

                // 핵심 설정 1: 완전 3D 사운드
                source.spatialBlend = 1f;

                // 핵심 설정 2: 거리에 따른 소리 감소 튜닝
                // minDistance 안에서는 소리가 최대 크기로 들립니다.
                // 이걸 50으로 잡으면, 보스가 50미터 안에 있으면 무조건 설정한 최대 볼륨으로 들립니다.
                source.minDistance = 50f;
                source.maxDistance = 500f; // 소리가 들리는 한계 거리
                source.rolloffMode = AudioRolloffMode.Linear; // 거리에 따라 정직하게 줄어듦

                source.Play();
                Destroy(audioObj, deathClip.length + 0.1f);
            }

            OnDead?.Invoke(this);
            Destroy(gameObject);
        }
    }

    // ⬇️ [!!! 핵심 추가 !!!] GameManager가 호출할 함수
    private float healthThreshold = -1f; // GameManager가 설정할 값

    /// <summary>
    /// GameManager가 이 보스에게 후속 보스 소환 임계값을 설정합니다.
    /// </summary>
    /// <param name="threshold">체력 비율 (예: 0.4)</param>
    public void SetHealthThreshold(float threshold)
    {
        healthThreshold = threshold;
        hasTriggeredThreshold = false; // 새 임계값이 설정되었으므로 리셋
        Debug.Log($"[{this.name}] Health Threshold가 {healthThreshold * 100}%로 설정되었습니다.");
    }
    // ⬆️ [!!! 핵심 추가 끝 !!!] ⬆️


    // --- (이하 화살 박힘 처리 로직 동일) ---
    public void RegisterStuckArrow(StickingArrowToSurface arrow)
    {
        if (arrow != null && !stuckArrows.Contains(arrow))
            stuckArrows.Add(arrow);
    }
    public void DetachAllStuckArrows()
    {
        for (int i = stuckArrows.Count - 1; i >= 0; i--)
        {
            if (stuckArrows[i] != null)
                stuckArrows[i].DetachAndFall();
        }
        stuckArrows.Clear();
    }
}