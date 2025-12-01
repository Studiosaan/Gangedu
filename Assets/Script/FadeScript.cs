using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FadeScript : MonoBehaviour
{
    public Image Panel;
    public float F_time = 1f;

    private void Awake()
    {
        if (Panel != null)
        {
            // 시작 시 투명하면 비활성화
            if (Panel.color.a <= 0.001f)
                Panel.gameObject.SetActive(false);
        }
    }

    public IEnumerator FadeOutCoroutine()
    {
        if (Panel == null) yield break;
        Panel.gameObject.SetActive(true);
        Panel.raycastTarget = true; // 클릭 차단
        Color c = Panel.color;
        float t = 0f;
        while (t < F_time)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(0f, 1f, t / F_time);
            Panel.color = c;
            yield return null;
        }
        c.a = 1f;
        Panel.color = c;
    }

    public IEnumerator FadeInCoroutine()
    {
        if (Panel == null) yield break;
        Panel.gameObject.SetActive(true);
        Panel.raycastTarget = true; // 페이드 중에는 클릭 막음
        Color c = Panel.color;
        float t = 0f;
        while (t < F_time)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(1f, 0f, t / F_time);
            Panel.color = c;
            yield return null;
        }
        c.a = 0f;
        Panel.color = c;
        Panel.raycastTarget = false; // 클릭 허용
        Panel.gameObject.SetActive(false); // 필요하면 비활성화
    }
}