using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MonsterCtrl : MonoBehaviour  
{

    public enum MonsterState { idle = 0, trace, attack, die }; // 몬스터 상태정보가 있는 Enumerable 변수 선언
    public MonsterState monsterState = MonsterState.idle; // 몬스터의 현재 상태 정보를 저정 할 Enum 변수

    private Transform monsterTr; // 몬스터 위치
    private Transform playerTr; // 추적할 플레이어 위치
    private NavMeshAgent nvAgent; // 추적 AI를 위한 네브메쉬
    private Animator animator; // 몬스터 애니메이션 업데이트를 위한 변수

    public float traceDist = 10.0f; // 추적 사정거리
    public float attackDist = 2.5f; // 공격 사정거리
    private bool isDie = false; // 몬스터 사망 여부
    private int hp = 100; // 몬스터 생명 변수

    // Start is called before the first frame update
    void Start()
    {
        monsterTr = this.gameObject.GetComponent<Transform>();
        playerTr = GameObject.FindWithTag("Player").GetComponent<Transform>();
        nvAgent = this.gameObject.GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();      
    }

    // Update is called once per frame
    void Update()
    {
        nvAgent.destination = playerTr.position; // 추적 위치할 위치를 업데이트 한다.
        animator.SetBool("IsTrace", true); // 추적 애니메이션을 활성화 한다.

        StartCoroutine(CheckMonsterState()); // 일정 간격으로 몬스터의 상태를 체크하는 코루틴
        StartCoroutine(MonsterAction()); // 몬스터의 상태에 따라 설정을 변경하는 코루틴

        this.transform.LookAt(new Vector3(playerTr.position.x, this.transform.position.y, playerTr.position.z));
    }

    // 일정한 간격으로 몬스터의 행동 상태를 체크하고 monsterState의 값 변경
    IEnumerator CheckMonsterState()
    {

        while(!isDie)
        {
            // 성능을 위해 0.2초 동안 기다렸다가 처리를 진행
            yield return new WaitForSeconds(0.2f);

            // 몬스터와 플레이어 사이의 거리 측정
            float dist = Vector3.Distance(playerTr.position, monsterTr.position);

            if(dist <= attackDist )
            {
                
                monsterState = MonsterState.attack;
            }
            else if(dist <= traceDist)
            {
                monsterState = MonsterState.trace;
            }
            else
            {
                monsterState = MonsterState.idle;
            }
        }

    }

    // 몬스터의 상태가밧에 따라 적절한 동작을 수행하는 함수
    IEnumerator MonsterAction()
    {
        while(!isDie)
        {
            switch(monsterState)
            {
                case MonsterState.idle:
                    nvAgent.isStopped = true; // 추적을 중단한다.
                    animator.SetBool("IsAttack", false);
                    animator.SetBool("IsTrace", false);
                    break;
                case MonsterState.trace:
                    nvAgent.destination = playerTr.position; // 추적 대상의 위치를 넘겨준다.
                    nvAgent.isStopped = false;// 추적을 시작한다
                    animator.SetBool("IsAttack", false);
                    animator.SetBool("IsTrace", true);
                    break;
                case MonsterState.attack:
                    nvAgent.isStopped = true;
                    animator.SetBool("IsAttack", true);
                    break;
            }
            yield return null;
        }
       
    }

    public void GetDamage(float amount)
    {
        hp -= (int)(amount / 2.0f); // 외계인은 데미지를 절반으로 줄이는 특수 능력
        animator.SetTrigger("IsHit");

        if(hp<=0)
        {
            MonsterDie();
        }
    }

    // 몬스터 사망 시 처리 루틴
    void MonsterDie() 
    {
        // 중복 실행 방지
        if(isDie == true)
        {
            return;
        }
        StopAllCoroutines(); // 모든 코루틴을 종료한다.
        isDie = true;
        monsterState = MonsterState.die;
        nvAgent.isStopped = true;
        animator.SetTrigger("IsDie");

        // 자신의 Collider를 비활성화
        gameObject.GetComponent<CapsuleCollider>().enabled = false;
        // 자식(Child)의 Collider를 비활성화
        foreach (Collider coll in gameObject.GetComponentsInChildren<SphereCollider>())
        {
            coll.enabled = false;
        }

    }
}
