using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerArrow : MonoBehaviour
{
    [SerializeField]
    private float attackAmount = 35.0f;


    void Start()
    {
        Destroy(gameObject, 5f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Monster"))
        {
            BulletSpawner bulletSpawner = other.GetComponent<BulletSpawner>();
            bulletSpawner?.GetDamage(attackAmount);
            Destroy(gameObject);
        }
    }
}
