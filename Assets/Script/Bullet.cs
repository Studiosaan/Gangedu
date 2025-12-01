using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField]
    private GameObject[] ringObjects; // bullet을 꾸며줄 링들
    private float startRange = -0.01f;
    private float endRange = -0.07f;

    private Rigidbody bulletRigidbody;
    public float bulletspeed { get; set; }

    private bool isSetup = false;

    private void Awake()
    {
        bulletRigidbody = GetComponent<Rigidbody>();
        if (bulletRigidbody == null)
        {
            bulletRigidbody = gameObject.AddComponent<Rigidbody>();
        }
        bulletRigidbody.useGravity = false;
        bulletRigidbody.isKinematic = false;
    }

    private void OnEnable()
    {
        // 혹시 프리팹이 비활성화 상태로 있었다면,
        // SetupBullet 전에 멈춰있게
        if (!isSetup && bulletRigidbody != null)
        {
            bulletRigidbody.velocity = Vector3.zero;
        }
    }

    public void SetupBullet(float speed)
    {
        bulletspeed = speed;
        isSetup = true;

        // 강제 활성화 (혹시 Instantiate 시 비활성화 상태로 복제되었을 경우 대비)
        gameObject.SetActive(true);

        Debug.Log($"[Bullet Setup] speed={bulletspeed}, active={gameObject.activeSelf}");

        // Rigidbody 속도 초기화
        if (bulletRigidbody == null)
            bulletRigidbody = GetComponent<Rigidbody>();

        bulletRigidbody.useGravity = false;
        bulletRigidbody.velocity = transform.forward * bulletspeed;

        // 링 초기화
        if (ringObjects != null && ringObjects.Length > 0)
        {
            for (int i = 0; i < ringObjects.Length; i++)
            {
                if (ringObjects[i] != null)
                    ringObjects[i].transform.localPosition = new Vector3(0f, 0f, startRange * (i + 1));
            }
        }

        StartCoroutine(RingMoveCoroutine());
        StartCoroutine(LifetimeCoroutine());
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController playercontroller = other.GetComponent<PlayerController>();
            if (playercontroller != null)
            {
                playercontroller.GetDamage(10f); // 💥 여기서 HP 감소 이벤트 발생!
            }

            gameObject.SetActive(false);
        }
        else if (other.CompareTag("Wall") || other.CompareTag("Ground"))
        {
            gameObject.SetActive(false);
        }
    }

    IEnumerator RingMoveCoroutine()
    {
        float percent = 0;
        float speed = 1.0f;

        while (percent < 1)
        {
            percent += Time.deltaTime * speed;
            float range = Mathf.Lerp(startRange, endRange * bulletspeed, percent);
            for (int i = 0; i < ringObjects.Length; i++)
            {
                if (ringObjects[i] != null)
                    ringObjects[i].transform.localPosition = new Vector3(0f, 0f, range * (i + 1));
            }
            yield return null;
        }
    }

    IEnumerator LifetimeCoroutine()
    {
        yield return new WaitForSeconds(5.0f);
        gameObject.SetActive(false);
    }
}
