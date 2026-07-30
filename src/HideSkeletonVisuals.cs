using UnityEngine;

public class HideSkeletonVisuals : MonoBehaviour
{
    [Header("Hide Settings")]
    public bool hideOnStart = true;
    public bool forceHideEveryFrame = true;

    void Start()
    {
        if (hideOnStart)
        {
            HideAllRenderers();
        }
    }

    void LateUpdate()
    {
        if (forceHideEveryFrame)
        {
            HideAllRenderers();
        }
    }

    [ContextMenu("Hide All Renderers")]
    public void HideAllRenderers()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = false;
        }
    }

    [ContextMenu("Show All Renderers")]
    public void ShowAllRenderers()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = true;
        }
    }
}