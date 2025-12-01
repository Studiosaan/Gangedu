using UnityEngine;

public class SkyboxChanger : MonoBehaviour
{
    // 1. 인스펙터 창에서 교체할 모든 스카이박스 머티리얼을 담을 배열입니다.
    public Material[] skyboxMaterials;

    // 2. 버튼에서 호출할 공개(public) 함수입니다.
    //    이 함수는 '몇 번째' 머티리얼로 바꿀지 인덱스(번호)를 받습니다.
    public void ChangeSkybox(int materialIndex)
    {
        // 3. 배열 범위를 벗어난 요청인지 확인합니다.
        if (materialIndex >= 0 && materialIndex < skyboxMaterials.Length)
        {
            // 4. (핵심) 씬의 스카이박스를 지정된 머티리얼로 즉시 교체합니다.
            RenderSettings.skybox = skyboxMaterials[materialIndex];

            // 5. (선택 사항) 스카이박스가 바뀌면 조명/반사도 갱신해주는 것이 좋습니다.
            DynamicGI.UpdateEnvironment();
        }
        else
        {
            Debug.LogWarning("SkyboxChanger: 잘못된 머티리얼 인덱스입니다. -> " + materialIndex);
        }
    }
}