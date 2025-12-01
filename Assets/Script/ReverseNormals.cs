using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReverseNormals : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        MeshFilter filter = GetComponent<MeshFilter>();
        Mesh mesh = filter.mesh;
        Vector3[] normals = mesh.normals;

        // 노말을 뒤집는다.
        for (int i = 0; i < normals.Length; i++)
            normals[i] = -normals[i];

        mesh.normals = normals;

        for (int m = 0; m < mesh.subMeshCount; m++) {
            // 메쉬의 폴리곤을 가져온다.
            int[] triangles = mesh.GetTriangles(m);
            for (int i = 0; i < triangles.Length; i += 3) {
                int temp = triangles[i + 0];
                triangles[i + 0] = triangles[i + 1]; // 시계 방향의 렌더링 순서를 반시계 방향으로 바꿈으로써 안에서 보이게 한다.
                triangles[i + 1] = temp;
            }
            mesh.SetTriangles(triangles, m);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
