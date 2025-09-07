using System.Collections.Generic;
using UnityEngine;

public sealed class UILinePathRendererAdapter : MonoBehaviour, IPathRenderer
{
    [SerializeField] private PathDrawerIconClamp drawer;

    public void DrawPath(IList<Vector2Int> cells)
    {
        if (drawer == null || cells == null) return;
        drawer.DrawPath(new List<Vector2Int>(cells));
    }

    public void Clear()
    {
        if (drawer == null) return;
        drawer.SendMessage("ClearAfter", 0f, SendMessageOptions.DontRequireReceiver);
    }
}
