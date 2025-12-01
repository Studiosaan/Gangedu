using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// using UnityEngine.UIElements; // ⬅️ (필요 없음)
using UnityEngine.XR.Interaction.Toolkit; // ⬅️ 진동을 위해 필요

public class PlayerController : MonoBehaviour
{
    [Header("XR 컨트롤러 참조")]
    [Tooltip("Hierarchy의 Left Hand Controller를 연결하세요.")]
    [SerializeField] private XRBaseController leftHandController;
    [Tooltip("Hierarchy의 Right Hand Controller를 연결하세요.")]
    [SerializeField] private XRBaseController rightHandController;

    [Header("진동 설정")]
    [SerializeField] private float hitAmplitude = 0.5f;
    [SerializeField] private float hitDuration = 0.15f;
    [SerializeField] private float deadAmplitude = 1.0f;
    [SerializeField] private float deadDuration = 0.5f;

    [Header("피격 효과 (10초 쿨타임)")]
    [Tooltip("맞았을 때 재생할 파티클 시스템 (자식 오브젝트)")]
    [SerializeField] private ParticleSystem hitVfx;
    [Tooltip("힘든 숨소리 오디오 클립")]
    [SerializeField] private AudioClip breathingSoundClip;
    [Tooltip("쨍그랑 소리 오디오 클립")]
    [SerializeField] private AudioClip glassSoundClip;
    [Tooltip("효과(파티클+소리) 쿨타임")]
    [SerializeField] private float hitEffectCooldown = 10.0f;

    // ⬇️ [!!! 1. 이 변수들을 추가하세요 !!!] ⬇️
    [Header("치유 효과")]
    [Tooltip("치유 시 재생할 파티클 시스템 (자식 오브젝트)")]
    [SerializeField] private ParticleSystem healVfx;
    [Tooltip("치유 시 재생할 오디오 클립")]
    [SerializeField] private AudioClip healSoundClip;
    // ⬆️ [!!! 1. 여기까지 추가 !!!] ⬆️

    private AudioSource playerSfxSource;
    private float lastHitEffectTime = -10.0f;

    [Header("플레이어 체력")]
    [SerializeField]
    private float maxHp = 1000.0f;
    [SerializeField]
    private float hp = 1000.0f;
    public float HP
    {
        get { return hp; }
    }

    public event Action<float, float> OnHealthChanged;

    private void Start()
    {
        hp = maxHp;
        OnHealthChanged?.Invoke(hp / maxHp, hp);

        playerSfxSource = GetComponent<AudioSource>();
        if (playerSfxSource == null)
        {
            playerSfxSource = gameObject.AddComponent<AudioSource>();
            playerSfxSource.playOnAwake = false;
            playerSfxSource.spatialBlend = 1.0f;
        }
        lastHitEffectTime = -hitEffectCooldown;
    }

    private void TriggerHaptics(float amplitude, float duration)
    {
        if (leftHandController != null)
        {
            leftHandController.SendHapticImpulse(amplitude, duration);
        }
        if (rightHandController != null)
        {
            rightHandController.SendHapticImpulse(amplitude, duration);
        }
    }

    public void GetDamage(float amount)
    {
        if (hp <= 0f) return;

        hp -= amount;
        bool isDead = false;

        if (hp < 0f)
        {
            hp = 0f;
            isDead = true;
        }

        OnHealthChanged?.Invoke(hp / maxHp, hp);

        if (!isDead)
        {
            if (Time.time > lastHitEffectTime + hitEffectCooldown)
            {
                Debug.Log("피격 쿨타임 효과 재생!");
                lastHitEffectTime = Time.time;

                if (hitVfx != null)
                {
                    hitVfx.Play();
                }

                if (playerSfxSource != null)
                {
                    if (breathingSoundClip != null)
                        playerSfxSource.PlayOneShot(breathingSoundClip);
                    if (glassSoundClip != null)
                        playerSfxSource.PlayOneShot(glassSoundClip);
                }
            }
        }

        if (isDead)
        {
            TriggerHaptics(deadAmplitude, deadDuration);
            Die();
        }
        else
        {
            TriggerHaptics(hitAmplitude, hitDuration);
        }
    }

    // ⬇️ [!!! 2. 이 함수를 새로 추가하세요 !!!] ⬇️
    /// <summary>
    /// 플레이어의 체력을 회복시킵니다.
    /// </summary>
    public void Heal(float amount)
    {
        if (hp <= 0f) return; // 죽었으면 회복 불가

        hp += amount;
        if (hp > maxHp)
        {
            hp = maxHp; // 최대 체력 초과 금지
        }

        Debug.Log($"플레이어 {amount} 회복! 현재 HP: {hp}");

        // 1. 치유 파티클 재생
        if (healVfx != null)
        {
            healVfx.Play();
        }

        // 2. 치유 소리 재생
        if (playerSfxSource != null && healSoundClip != null)
        {
            playerSfxSource.PlayOneShot(healSoundClip);
        }

        // 3. [중요] UI 갱신
        OnHealthChanged?.Invoke(hp / maxHp, hp);
    }
    // ⬆️ [!!! 2. 여기까지 추가 !!!] ⬆️

    public void Die()
    {
        gameObject.SetActive(false);
    }
}