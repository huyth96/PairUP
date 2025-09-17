using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GraphicRaycasterDelay : MonoBehaviour
{
    [SerializeField] private float delaySeconds = 0.25f;
    [SerializeField] private GraphicRaycaster raycaster;

    IEnumerator Start()
    {
        if (!raycaster) raycaster = GetComponent<GraphicRaycaster>();
        if (raycaster)
        {
            raycaster.enabled = false;                        // chặn mọi click
            yield return new WaitForSecondsRealtime(delaySeconds);
            raycaster.enabled = true;                         // mở lại sau khi “hết đà” chạm
        }
    }
}
