/*
 * Copyright (c) 2017 VR Stuff
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 이 스크립트는 Unity 씬에서 '차원(Dimension)' 역할을 하는 게임 오브젝트에 붙어,
// 레이어(Layer) 관리 및 스카이박스 설정 등 차원 전환 시스템의 핵심 기능을 수행합니다.
public class Dimension : MonoBehaviour
{
    // 인스펙터 창에서 할당할 사용자 정의 스카이박스 Material입니다. 
    // 이 차원으로 전환될 때 메인 카메라에 적용됩니다.
    public Material customSkybox;

    // 이 차원에 할당된 고유한 레이어 번호입니다. 
    // (HideInInspector: 인스펙터 창에서 숨김)
    [HideInInspector]
    public int layer;

    // [Tooltip: 인스펙터 창에서 마우스를 올리면 표시되는 설명]
    // 이 차원이 경험이 시작되는 '초기 차원'임을 지정합니다.
    [Tooltip("This designates this dimension as the original dimension from which the experience will start.")]
    public bool initialWorld = false;

    // 이 옵션을 true로 설정하면, 렌더링 시에만 레이어를 변경하고 
    // 평소에는 초기 레이어 상태를 유지합니다. 
    // 경고: 이는 물리적 상호작용(Rigidbody 충돌 등)에 영향을 줄 수 있습니다.
    [Tooltip("This forces the Dimension to only affect the layers during rendering (thus keeping things like raycasting intact). Warning: This will break the automatic physics adjustment that keeps you from hitting things in other dimensions.")]
    public bool forceKeepInitialLayers = false;

    // 이 차원에 연결된 Portal 스크립트 목록입니다.
    [HideInInspector]
    public List<Portal> connectedPortals;

    // 현재 사용되는 카메라 인스턴스 (사용되지 않을 수 있음)
    [HideInInspector]
    public Camera cam;

    // PreRender/PostRender 시 레이어 복구를 위해, 
    // 임시로 레이어가 변경된 자식 오브젝트들의 원래 레이어를 저장하는 딕셔너리입니다.
    // Key: GameObject Instance ID, Value: Original Layer
    private Dictionary<int, int> layerSwitchedChildren;

    // 메인 카메라 설정이 필요한지 여부를 나타내는 플래그입니다. 
    // 주로 VRTK와 같은 툴킷에서 메인 카메라가 늦게 활성화될 경우를 대비합니다.
    private bool mainCameraNeedsSetup = true;

    // 게임 오브젝트가 처음 로드될 때 호출됩니다.
    void Awake()
    {
        // 연결된 포털 목록 초기화
        connectedPortals = new List<Portal>();

        // LayerManager를 통해 이 차원에 대한 고유 레이어를 생성하고 번호를 할당합니다.
        // 또한 LayerManager의 정의된 차원 목록에 이 차원을 추가합니다.
        layer = LayerManager.Instance().CreateLayer(gameObject.name);
        LayerManager.definedDimensions.Add(this);

        // 현재 Dimension 게임 오브젝트 자체의 레이어를 새로 생성된 레이어로 설정합니다.
        gameObject.layer = layer;

        // "Default" 레이어 번호를 가져옵니다.
        int defaultLayer = LayerMask.NameToLayer("Default");

        // forceKeepInitialLayers가 false일 경우 (기본값)
        if (!this.forceKeepInitialLayers)
        {
            // 이 오브젝트의 모든 자식 오브젝트를 순회합니다.
            Transform[] childrenTransforms = gameObject.GetComponentsInChildren<Transform>();
            foreach (Transform t in childrenTransforms)
            {
                // 자식 오브젝트의 레이어를 이 차원의 고유 레이어로 설정합니다.
                t.gameObject.layer = layer;

                // 만약 Light 컴포넌트가 있다면
                if (t.gameObject.GetComponent<Light>())
                {
                    Light light = t.gameObject.GetComponent<Light>();
                    // 해당 라이트가 Default 레이어와 이 차원의 고유 레이어만 비추도록 설정합니다.
                    light.cullingMask = defaultLayer;
                    light.cullingMask |= 1 << layer; // 비트 OR 연산으로 레이어 추가
                }
            }
        }

        // 씬에 있는 모든 카메라를 순회하며 초기 렌더링 설정을 합니다.
        foreach (Camera camera in Camera.allCameras)
        {
            if (this.initialWorld)
            {
                // 이 차원이 초기 차원이라면, 카메라가 이 레이어를 렌더링하도록 설정합니다.
                CameraExtensions.LayerCullingShow(camera, layer);

                // 카메라에 Skybox 컴포넌트가 있다면
                if (camera.GetComponent<Skybox>())
                {
                    // 할당된 사용자 정의 스카이박스 Material을 적용합니다.
                    camera.GetComponent<Skybox>().material = customSkybox;
                }
            }
            else
            {
                // 초기 차원이 아니라면, 카메라가 이 레이어를 렌더링하지 않도록 설정합니다.
                CameraExtensions.LayerCullingHide(camera, layer);
            }
        }
    }

    // 초기화 시점 (Awake 다음에 호출됨)
    void Start()
    {

    }

    // 매 프레임마다 호출됩니다.
    void Update()
    {
        /* This is used to enable VRTK kit builds*/

        // 메인 카메라 설정이 아직 되지 않았다면 시도합니다. (주로 늦게 활성화되는 VR 카메라 처리)
        if (mainCameraNeedsSetup)
        {
            // Camera.main이 null이면 아직 메인 카메라가 준비되지 않은 것입니다.
            if (Camera.main == null)
            {
                return;
            }

            // 메인 카메라에 대한 초기 차원 설정을 적용합니다. (Awake에서의 설정과 동일)
            if (this.initialWorld)
            {
                CameraExtensions.LayerCullingShow(Camera.main, layer);
                // 메인 카메라 자체도 이 차원의 레이어로 설정합니다.
                Camera.main.gameObject.layer = layer;

                if (Camera.main.GetComponent<Skybox>())
                {
                    Camera.main.GetComponent<Skybox>().material = customSkybox;
                }
            }
            else
            {
                CameraExtensions.LayerCullingHide(Camera.main, layer);
            }

            // 설정이 완료되었으므로 플래그를 false로 변경합니다.
            this.mainCameraNeedsSetup = false;
        }
    }

    // 사용자가 이 차원에 진입했을 때 호출됩니다.
    // 이 차원으로 연결되는 모든 포털들의 목적지를 서로 전환시킵니다.
    public void SwitchConnectingPortals()
    {
        foreach (Portal portal in connectedPortals)
        {
            // 포털의 목적지(ToDimension)가 현재 이 차원인 경우
            if (portal.ToDimension() == this)
            {
                // 포털의 연결된 차원을 서로 바꿉니다. (사용자가 포털을 다시 통과하여 돌아갈 수 있도록)
                portal.SwitchPortalDimensions();
            }
        }
    }

    // 특정 태그를 가진 자식 오브젝트들을 보이게(Default 레이어로 전환) 합니다.
    public void showChildrenWithTag(string tag)
    {
        if (tag == "" || tag == null)
        {
            return;
        }

        int defaultLayer = LayerMask.NameToLayer("Default");
        Transform[] childrenTransforms = gameObject.GetComponentsInChildren<Transform>();
        foreach (Transform t in childrenTransforms)
        {
            // 태그가 일치하면
            if (t.CompareTag(tag))
            {
                // 해당 오브젝트의 레이어를 Default 레이어로 설정하여 보이게 합니다.
                t.gameObject.layer = defaultLayer;
            }
        }
    }


    // 렌더링 직전에 호출됩니다. forceKeepInitialLayers가 true일 경우에만 작동합니다.
    // (예: 포털 렌더링을 위해 임시로 이 차원의 모든 오브젝트를 고유 레이어로 변경)
    public void PreRender()
    {
        if (!forceKeepInitialLayers)
            return;

        if (layerSwitchedChildren == null)
        {
            layerSwitchedChildren = new Dictionary<int, int>();
        }

        layerSwitchedChildren.Clear();

        int defaultLayer = LayerMask.NameToLayer("Default");
        Transform[] childrenTransforms = gameObject.GetComponentsInChildren<Transform>();
        foreach (Transform t in childrenTransforms)
        {
            // 원래 레이어를 저장합니다.
            layerSwitchedChildren.Add(t.gameObject.GetInstanceID(), t.gameObject.layer);

            // 레이어를 이 차원의 고유 레이어로 변경합니다. (렌더링을 위해)
            t.gameObject.layer = layer;

            // 라이트의 Cull Mask도 렌더링에 맞게 조정합니다.
            if (t.gameObject.GetComponent<Light>())
            {
                Light light = t.gameObject.GetComponent<Light>();
                light.cullingMask = defaultLayer;
                light.cullingMask |= 1 << layer;
            }
        }
    }

    // 렌더링 직후에 호출됩니다. forceKeepInitialLayers가 true일 경우에만 작동합니다.
    // (PreRender에서 임시로 변경했던 레이어를 원래대로 복구)
    public void PostRender()
    {
        if (!forceKeepInitialLayers)
            return;

        Transform[] childrenTransforms = gameObject.GetComponentsInChildren<Transform>();
        foreach (Transform t in childrenTransforms)
        {
            // 저장된 원래 레이어 값을 가져옵니다.
            int layer = layerSwitchedChildren[t.gameObject.GetInstanceID()];

            // 레이어를 원래 값으로 복구합니다.
            t.gameObject.layer = layer;
        }
    }
}