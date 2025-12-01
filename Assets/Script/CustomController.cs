using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public enum HandState {NONE =0,RIGHT,LEFT };
public class CustomController : MonoBehaviour
{
    // device 특성값을 담는 변수
    public InputDeviceCharacteristics characteristics;
    [SerializeField]
    private List<GameObject> controllerModels; // 사용 가능한 모델 리스트들
    private GameObject controllerInstance; // 생성된 컨트롤러 인스턴스를 참조하는 변수
    private InputDevice availableDevice; // 현재 사용중인 컨트롤러

    public bool renderController; // Hand와 Controller 사이를 변경할 변수
    public GameObject handModel; // 핸드 모델 prefab
    private GameObject handInstance; // 생성된 핸드 인스턴스를 참조하는 변수

    private Animator handModelAnimator; // 핸드 모델 애니메이션 변수

    public GameObject handGun; // 총 모델

    bool triggerButton; // 총알 단발 발사용 불리언

    public HandState currentHand; // 현재 오른손,왼손인지 알기 위한 변수

    void Start()
    {
        TryInitiaiize();
    }
    void Update()
    {
        // 모종의 이유로 퀘스트가 셋팅이 되지 않았다면 다시 초기화를 진행한다.
        if (!availableDevice.isValid)
        {
            TryInitiaiize();
        }

        // 핸드 모델과 컨트롤러 모델중 선택한다.
        if (renderController)
        {
            handInstance.SetActive(false);
            controllerInstance.SetActive(true);
        }
        else
        {
            handInstance.SetActive(true);
            controllerInstance.SetActive(false);
            UpdateHandAnimation(); // 핸드 애니메이션은 여기서만 수행한다.

        }

        if(handGun != null) // 총 모델이 있다면 트리거 버튼이 눌렸을때 발사한다.
        {
            bool triggerButtonValue;
            if(availableDevice.TryGetFeatureValue(CommonUsages.triggerButton,out triggerButtonValue) && triggerButtonValue)
            {
                if(triggerButton == false && currentHand == handGun.GetComponent<GunShoot>().currentGrab) // 트리거 버튼이 눌리지 않은 상태에서만 발사된다.
                {
                    handGun.GetComponent<GunShoot>().Shoot();
                    triggerButton = true;
                }
                
            }
            else
            {
                triggerButton = false;
            }
        }

        if(true) // GameManager.Instance.isGameOver 테스트시에만
        {
            bool menuButtonValue;

            if (availableDevice.TryGetFeatureValue(CommonUsages.menuButton, out menuButtonValue) && menuButtonValue)
            {
                GameManager.Instance.RestartGame();
            }
         }
    }

    void TryInitiaiize()
    {
        List<InputDevice> devices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(characteristics, devices);

        foreach(var device in devices)
        {
            Debug.Log($"Available Device Name : {device.name}, Characteristic  {device.characteristics}");
        }
        if(devices.Count > 0)
        {
            availableDevice = devices[0];
            GameObject currentControllerModel;

            // Left 및 Right에 따라서 컨트롤의 손을 각각 설정
            if(availableDevice.name.Contains("Left"))
            {
                currentControllerModel = controllerModels[1];
                currentHand = HandState.LEFT;
            }
            else if(availableDevice.name.Contains("Right"))
            {
                currentControllerModel = controllerModels[2];
                currentHand = HandState.RIGHT;

            }
            else
            {
                currentControllerModel = null;
                currentHand = HandState.NONE;
            }
            if(currentControllerModel)
            {
                controllerInstance = Instantiate(currentControllerModel, transform);
            }
            else
            {
                // 적당한 객체를 찾지 못하면 기본 객체를 생성한다.
                Debug.LogError("Didn't get sutiable controller model");
                controllerInstance = Instantiate(controllerModels[0], transform);
            }

            handInstance = Instantiate(handModel, transform); // 핸드 인스턴스 추가
            handModelAnimator = handInstance.GetComponent<Animator>(); // 핸드 인스턴스에 추가되어 있는 애니메이터를 가져온다.
        }
    }

    void UpdateHandAnimation()
    {
        // 컨트롤러의 trigger 값을 애니메이터의 trigger 값에 대입한다.
      
        if (availableDevice.TryGetFeatureValue(CommonUsages.trigger,out float triggerValue))
        {
            handModelAnimator.SetFloat("Trigger", triggerValue);
        }
        else
        {
            handModelAnimator.SetFloat("Trigger", 0);
        }

        // 컨트롤러의 grip 값을 애니메이터의 grip 값에 대입한다.
        if (availableDevice.TryGetFeatureValue(CommonUsages.grip,out float gripValue))
        {
            handModelAnimator.SetFloat("Grip", gripValue);
        }
        else
        {
            handModelAnimator.SetFloat("Grip", 0);
        }
    }
}
