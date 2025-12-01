using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class MovementProvider : MonoBehaviour
{
    public float speed = 1.0f; // 이동 속도
    public float gravityMultiplier = 1.0f; // 중력에 영향을 받는 경우를 처리
    public List<XRController> controllers = null; // 컨트롤러 리스트 (상황에 따라서 1개 혹은 n개가 설정 될 수 있다.)
    private CharacterController characterController = null; // VR Rig의 캐릭터 컨트롤러
    private GameObject head = null; // 카메라의 헤드 위치

    private void Awake() // 스크립트 실행시 가장 먼저 한번 실행, setActive(false) 상태여도 실행 된다.
    {
        characterController = GetComponent<CharacterController>(); // 캐릭터 콘트롤러 할당
        head = GetComponent<XRRig>().cameraGameObject; // 등록되어 있는 player Camera 객체를 가져온다.
    }

    // Start is called before the first frame update
    void Start()
    {
        // 초기 설정을 처리
        PositionController();
    }

    // 이동 처리가 입력보다 먼저 실행된다, 입력에 즉각적으로 화면이 갱신되면 어지럽기 때문이다. 
    void Update()
    {
        PositionController(); // 현재 위치에 맞게 위치를 설정함
        CheckForInput(); // 설정된 컨트롤러 중에서 인풋 입력이 있다면 이동처리를 함
        //ApplyGravity(); // 밑으로 떨어지는 경우 중력을 작용
    }

    void PositionController()
    {
        // 부드러운 이동을 위해서 1(최소) 2(최대)에서 head의 y 값이 결정 되도록 함! 
        float headHeight = Mathf.Clamp(head.transform.localPosition.y, 0.1f, 0.1f);
        characterController.height = headHeight;  

        Vector3 newCenter = Vector3.zero;
        newCenter.x = head.transform.localPosition.x;
        newCenter.z = head.transform.localPosition.z;

        characterController.center = newCenter; // 캐릭터의 위치는 머리의 x,z 좌표
    }

    void CheckForInput()
    {
        if(!GameManager.Instance.Isworld_1) // Park에서는 움직일 수 없다.
        {
            foreach (XRController controller in controllers)
            {
                if (controller.enableInputActions) // 컨트롤러에서 액션이 발생했다면
                {
                    CheckForMovement(controller.inputDevice);
                }
            }
        }

    }

    void CheckForMovement(InputDevice device)
    {
        // 레버는 primary2DAxis 값으로 읽어 올수 있다.
        if (device.TryGetFeatureValue(CommonUsages.primary2DAxis,out Vector2 position))
        {
            StartMove(position);
        }
    }
    void StartMove(Vector2 position)
    {
        Vector3 direction = new Vector3(position.x, 0, position.y); // y 축은 영향 받지 않는다.
        Vector3 headRotation = new Vector3(0, head.transform.eulerAngles.y, 0); // Player Camera의 회전 방향을 적용

        direction = Quaternion.Euler(headRotation) * direction; // Player Camera가 바라보는 방향으로 회전한다.

        Vector3 movement = direction * speed;
        characterController.Move(movement * Time.deltaTime);
    }
    void ApplyGravity()
    {
        // 떨어지고 있는 상황에서 중력이 적용된 벡터
        Vector3 gravity = new Vector3(0, Physics.gravity.y * gravityMultiplier, 0);
        gravity.y += Time.deltaTime;

        characterController.Move(gravity * Time.deltaTime);
    }
}
