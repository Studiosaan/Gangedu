using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoFront : MonoBehaviour
{
    [SerializeField]
    private float speed = 0.05f;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.Translate((transform.up) * Time.deltaTime * speed);
    }
    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("MoveEnd"))
        {
            GameManager.Instance.WorldChange();
        }
    }
}
