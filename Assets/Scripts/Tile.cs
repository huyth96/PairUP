using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Tile : MonoBehaviour, IPointerClickHandler
{
    public int row, col;
    public int id;                 // cùng id => cùng loại
    public bool removed;

    [Header("Refs")]
    public Image bg;               // ảnh ô vuông (có thể để null)
    public Image icon;             // drag Image con "Icon" vào

    public System.Action<Tile> onClicked;

    public void Setup(int r, int c, int id_, Sprite sprite)
    {
        row = r; col = c; id = id_; removed = false;
        if (icon != null)
        {
            icon.sprite = sprite;
            icon.enabled = true;
        }
        gameObject.SetActive(true);
    }

    public void Clear()
    {
        removed = true;
        if (icon != null) icon.enabled = false;  // ẩn icon
        if (bg != null) bg.enabled = false;
                                                
    }


    public void OnPointerClick(PointerEventData e)
    {
        if (!removed) onClicked?.Invoke(this);
    }
}
