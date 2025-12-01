using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StickingArrowToSurface : MonoBehaviour
{
    [SerializeField]
    private Rigidbody rb;
    [SerializeField]
    private SphereCollider myCollider;

    // (선택) 시각용 별도 프리팹 대신 원본을 그대로 사용하도록 수정
    // [SerializeField]
    // private GameObject stickingArrow;

    // 박힌 상태 표시용 플래그
    private bool isStuck = false;

    // 붙어있을 때 움직이지 않도록 비활성화할 컴포넌트가 있다면 할당(예: 화살 비행 제어 스크립트)
    // private MonoBehaviour flightScript;

    private void OnCollisionEnter(Collision collision)
    {
        if (isStuck) return;

        // 멈추고 충돌을 트리거로 변경 (물리로는 고정)
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (myCollider != null)
            myCollider.isTrigger = true;

        // 부모 설정: 충돌한 객체에 붙인다.
        if (collision.collider != null)
        {
            // 부모를 rigidbody가 있으면 그것의 transform, 없으면 collider.transform
            Transform parentTransform = collision.collider.attachedRigidbody != null
                ? collision.collider.attachedRigidbody.transform
                : collision.collider.transform;

            transform.parent = parentTransform;
        }

        isStuck = true;

        // 충돌 대상이 IHittable이면 데미지 처리 호출
        collision.collider.GetComponent<IHittable>()?.GetHit();

        // 만약 충돌 대상이 BulletSpawner라면 등록 요청
        BulletSpawner spawner = null;
        if (collision.collider.attachedRigidbody != null)
            spawner = collision.collider.attachedRigidbody.GetComponent<BulletSpawner>();
        if (spawner == null)
            spawner = collision.collider.GetComponent<BulletSpawner>();

        if (spawner != null)
        {
            spawner.RegisterStuckArrow(this);
        }

        // 추가: 더 이상 자체 업데이트가 필요 없으면 비활성화(예: flightScript가 있다면)
        // if (flightScript != null) flightScript.enabled = false;
    }

    // 외부에서 호출하여 이 화살을 분리하고 중력을 적용해 떨어지게 함
    public void DetachAndFall()
    {
        if (!isStuck) return;

        // 부모에서 분리
        transform.parent = null;

        // Rigidbody를 활성화하여 중력 적용
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        if (myCollider != null)
        {
            myCollider.isTrigger = false;
        }

        isStuck = false;

        // (선택) 일정 시간 후 자동 삭제 등 추가 처리 가능
    }
}