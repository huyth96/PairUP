using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Collections.Generic;
using System.Collections;

public class Tile : MonoBehaviour, IPointerClickHandler, ITileView
{
    public int row, col;
    public int id;
    public bool removed;
    private Vector3 originalScale;
    private Color originalColor;
    // Tile.cs — thêm vào trong class
    private bool _hintOn;
    private Color _originalBg;
    [Header("Refs")]
    public Image bg;
    public Image icon;

    public event Action<ITileView> OnClicked;

    public int Row => row;
    public int Col => col;
    public int Id => id;
    public bool IsRemoved => removed;
    public Image Icon => icon;

    void Awake()
    {
        originalScale = transform.localScale;
        if (bg != null) { originalColor = bg.color; _originalBg = bg.color; }
    }
    public void SetHint(bool on)
    {
        _hintOn = on;
        if (bg != null)
            bg.color = on ? new Color(0.2f, 1f, 1f, 1f) : _originalBg; // cyan nhạt
        if (on)
            transform.localScale = originalScale * 1.1f;
        else
            transform.localScale = originalScale;
    }

    // (không bắt buộc) blink nhẹ để thu hút mắt
    public IEnumerator BlinkHint(float duration = 1.2f, float freq = 6f)
    {
        SetHint(true);
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = 0.5f + 0.5f * Mathf.Sin(t * freq);
            if (bg != null) bg.color = Color.Lerp(new Color(0.2f, 1f, 1f, 0.8f), new Color(0.2f, 1f, 1f, 1f), a);
            yield return null;
        }
        SetHint(false);
    }
    public void SetSelected(bool selected)
    {
        if (selected)
        {
            // scale lên 1.2x
            transform.localScale = originalScale * 1.2f;
            if (bg != null) bg.color = Color.yellow;
        }
        else
        {
            // trả lại trạng thái ban đầu
            transform.localScale = originalScale;
            if (bg != null) bg.color = originalColor;
        }
    }
    public void PlayClearAnimation(float duration = 0.2f)
    {
        StartCoroutine(ClearAnim(duration));
    }

    private IEnumerator ClearAnim(float duration)
    {
        Vector3 startScale = transform.localScale;
        Vector3 endScale = Vector3.zero;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, endScale, t / duration);
            yield return null;
        }

        // ẩn hẳn sau khi anim xong
        Hide();
        transform.localScale = startScale; // reset scale cho lần spawn sau
    }

    public void Setup(int r, int c, int id_, Sprite sprite)
    {
        row = r; col = c; id = id_; removed = false;
        if (icon != null) { icon.sprite = sprite; icon.enabled = true; }
        if (bg != null) bg.enabled = true;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        removed = true;
        if (icon != null) icon.enabled = false;
        if (bg != null) bg.enabled = false;
    }

    public void Show(Sprite s)
    {
        removed = false;
        if (icon != null) { icon.sprite = s; icon.enabled = true; }
        if (bg != null) bg.enabled = true;
        gameObject.SetActive(true);
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (!removed)
        {
            Debug.Log($"Tile clicked: row={row}, col={col}, id={id}");
            OnClicked?.Invoke(this);
        }
    }
    public void SetIdAndIcon(int newId, Sprite sprite)
    {
        id = newId;
        removed = false;
        if (icon != null) { icon.sprite = sprite; icon.enabled = true; }
        if (bg != null) bg.enabled = true;
    }
}
