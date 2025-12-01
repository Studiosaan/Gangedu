using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
public class GunShoot : MonoBehaviour
{
    public GameObject bulletPrefab;
    public GameObject casingPrefab;
    public GameObject muzzleFlashPrefab;
    public Transform barrelLocation;
    public Transform casingExitLocation;

    public float shotPower = 100f;

    public bool isGrab = false;
    Animator childAnimator;

    public AudioClip fireClip; // 총 발사 사운드 클립
    AudioSource fireAudio; // 발사 사운드를 재생할 오디오소스 컴포넌트

    public HandState currentGrab; // CustomController에서 정의한다.

    // Start is called before the first frame update
    void Start()
    {
        if (barrelLocation == null)
        {
            barrelLocation = transform;
        }
        childAnimator = GetComponentInChildren<Animator>();
        fireAudio = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
    }
    public void GrabGun()
    {
        isGrab = true;
    }
    public void DropGun()
    {
        isGrab = false;
    }

    public void SetGrapState(HandState state)
    {
        currentGrab = state;
    }

    public void SetGrapNone()
    {
        if (!GetComponent<XRGrabInteractable>().isSelected)
        {
            currentGrab = HandState.NONE;
        }
    }

    public void SetGrapLeft()
    {
        currentGrab = HandState.LEFT;
    }
    public void SetGrapRight()
    {
        currentGrab = HandState.RIGHT;
    }

    public void Shoot()
    {
        if(isGrab == true) // 총을 잡았을때만 사격
        {
            GameObject tempFlash;
            Instantiate(bulletPrefab, barrelLocation.position, barrelLocation.rotation).GetComponent<Rigidbody>().AddForce(barrelLocation.forward * shotPower);
            tempFlash = Instantiate(muzzleFlashPrefab, barrelLocation.position, barrelLocation.rotation); // 사격 이펙트 생성

            childAnimator?.SetTrigger("Fire"); // 사격 애니메이션 실행
            fireAudio?.PlayOneShot(fireClip); // 사격 사운드 실행
            CasingRelease(); 
        }
    }


    void CasingRelease()
    {
        GameObject casing;
        casing = Instantiate(casingPrefab, casingExitLocation.position, casingExitLocation.rotation) as GameObject;
        casing.GetComponent<Rigidbody>().AddExplosionForce(550f, (casingExitLocation.position - casingExitLocation.right * 0.3f - casingExitLocation.up * 0.6f), 1f);
        casing.GetComponent<Rigidbody>().AddTorque(new Vector3(0, Random.Range(100f, 500f), Random.Range(10f, 1000f)), ForceMode.Impulse);
    }
}
