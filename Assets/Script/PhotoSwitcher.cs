using System;
using System.Collections; // 코루틴을 위해 필요
using UnityEngine;

public class PhotoSwitcher : MonoBehaviour
{
    [Header("사진 및 버튼 설정")]
    public Material[] skyboxMaterials;
    public GameObject[] photoContainers;
    public Renderer photoSphereRenderer;

    [Header("페이드 효과 연결")]
    public FadeScript fadeScript; // FadeScript를 연결할 슬롯

    [Header("초기 설정")]
    // ⬇️ [추가됨] 처음에 숨기고 싶은 버튼을 여기에 연결하세요
    public GameObject buttonToHideOnStart;

    // 외부 구독용 이벤트
    public event Action<int> OnPhotoChanged; // 페이드 아웃 후 사진을 실제로 바꾼 직후(검정 화면 상태)
    public event Action<int> OnPhotoChangeCompleted; // 페이드 인이 끝난 직후

    public int CurrentIndex { get; private set; } = 0;
    public int PreviousIndex { get; private set; } = 0;

    void Start()
    {
        // 씬 시작 시 즉시 0번 사진을 설정합니다.
        ApplyPhotoChange(0);

        // ⬇️ [추가됨] 0번 컨테이너가 켜진 뒤, 특정 버튼만 콕 집어서 끕니다.
        if (buttonToHideOnStart != null)
        {
            buttonToHideOnStart.SetActive(false);
        }

        // 씬 시작 시 Fade-In 처리
        if (fadeScript != null)
        {
            Color alpha = fadeScript.Panel.color;
            alpha.a = 1f;
            fadeScript.Panel.color = alpha;
            fadeScript.Panel.gameObject.SetActive(true);

            StartCoroutine(fadeScript.FadeInCoroutine());
        }
    }

    // 버튼에서 호출하는 공개 함수 (인덱스 유효성 검사 후 코루틴 실행)
    public void ChangeSkybox(int index)
    {
        if (index < 0 || index >= skyboxMaterials.Length || index >= photoContainers.Length || fadeScript == null)
        {
            Debug.LogWarning("PhotoSwitcher: 인덱스가 잘못되었거나 FadeScript가 연결되지 않았습니다.");
            return;
        }

        StartCoroutine(FadeAndChangeCoroutine(index));
    }

    private IEnumerator FadeAndChangeCoroutine(int index)
    {
        // Fade Out
        yield return StartCoroutine(fadeScript.FadeOutCoroutine());

        // 사진 및 버튼 그룹 교체 (화면은 아직 검음)
        ApplyPhotoChange(index);

        // 이 시점에 이벤트 발생(게임매니저가 이 타이밍에 오브젝트 소환하면 팝인 없이 처리 가능)
        OnPhotoChanged?.Invoke(index);

        // Fade In
        yield return StartCoroutine(fadeScript.FadeInCoroutine());

        // 완료 이벤트
        OnPhotoChangeCompleted?.Invoke(index);
    }

    // 외부에서 즉시(페이드 없이) 사진을 설정하도록 허용 — GameManager가 페이드 루틴 내부에서 사용
    public void SetPhotoImmediate(int index)
    {
        ApplyPhotoChange(index);
    }

    // 실제 사진/버튼 실무 처리
    private void ApplyPhotoChange(int index)
    {
        if (index < 0 || index >= skyboxMaterials.Length || index >= photoContainers.Length)
        {
            Debug.LogWarning("PhotoSwitcher: ApplyPhotoChange에서 잘못된 인덱스입니다: " + index);
            return;
        }

        // 이전 인덱스 저장
        PreviousIndex = CurrentIndex;
        CurrentIndex = index;

        if (photoSphereRenderer != null)
        {
            photoSphereRenderer.material = skyboxMaterials[index];
        }

        for (int i = 0; i < photoContainers.Length; i++)
        {
            if (photoContainers[i] != null)
                photoContainers[i].SetActive(false);
        }

        if (photoContainers[index] != null)
            photoContainers[index].SetActive(true);
    }
    // [추가됨] 숨겨놨던 버튼을 다시 켜주는 함수
    public void ShowHiddenButton()
    {
        if (buttonToHideOnStart != null)
        {
            buttonToHideOnStart.SetActive(true);
        }
    }
}