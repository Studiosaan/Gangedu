using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArrowController : MonoBehaviour
{
    [SerializeField]
    private GameObject midPointVisual, arrowPrefab, arrowSpawnPoint;

    [SerializeField]
    private float arrowMaxSpeed = 10;

    [SerializeField]
    private AudioSource bowReleaseAudioSource;

    [Header("Arrow Pooling")]
    [Tooltip("필드에 남을 수 있는 최대 화살 개수")]
    [SerializeField]
    private int maxArrowsInField = 8;

    // 전체 필드 기준으로 생성된 화살을 추적하는 큐 (가장 먼저 생성된 화살을 먼저 제거)
    private static Queue<GameObject> activeArrows = new Queue<GameObject>();

    public void PrepareArrow()
    {
        midPointVisual.SetActive(true);
    }

    public void ReleaseArrow(float strength)
    {
        bowReleaseAudioSource.Play();
        midPointVisual.SetActive(false);

        GameObject arrow = Instantiate(arrowPrefab);
        arrow.transform.position = arrowSpawnPoint.transform.position;
        arrow.transform.rotation = midPointVisual.transform.rotation;
        Rigidbody rb = arrow.GetComponent<Rigidbody>();
        rb.AddForce(midPointVisual.transform.forward * strength * arrowMaxSpeed, ForceMode.Impulse);

        // 큐에 추가하기 전에 null(파괴된 항목) 정리
        CleanNullEntriesFromQueue();

        // 새 화살 등록
        activeArrows.Enqueue(arrow);

        // 초과한 화살이 있으면 가장 오래된 화살부터 제거
        while (activeArrows.Count > maxArrowsInField)
        {
            GameObject oldest = activeArrows.Dequeue();
            if (oldest != null)
            {
                Destroy(oldest);
            }
        }
    }

    // 큐 앞쪽에 이미 파괴된(=null) 항목이 남아있으면 제거
    private void CleanNullEntriesFromQueue()
    {
        while (activeArrows.Count > 0 && activeArrows.Peek() == null)
        {
            activeArrows.Dequeue();
        }
    }
}