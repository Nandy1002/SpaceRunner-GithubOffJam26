using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System;

[RequireComponent(typeof(CanvasGroup))]
public class GravityChangerUI : MonoBehaviour
{
    [SerializeField] private Image cursor;
    [SerializeField] private Image cursorMoveBorder;
    [SerializeField] private List<Image> gravityImages = new List<Image>();
    [SerializeField, Range(0.01f, 1f)] private float wheelOpenTimeScale = 0.1f;

    private CanvasGroup canvasGroup;
    private RectTransform cursorRectTransform;
    private RectTransform cursorMoveBorderRectTransform;
    private Canvas cursorCanvas;
    private Canvas cursorMoveBorderCanvas;
    private Vector3[] cursorCorners = new Vector3[4];
    private Vector3[] gravityImageOriginalScales;
    private int hoveredGravityIndex = -1;
    private int selectedGravityIndex = -1;
    private bool gravityWheelOpen;
    private float previousTimeScale = 1f;
    private float previousFixedDeltaTime = 0.02f;
    private Animator[] wheelAnimators;
    private AnimatorUpdateMode[] previousAnimatorUpdateModes;

    public event Action<int> GravitySelected;

    public int SelectedGravityIndex => selectedGravityIndex;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        wheelAnimators = GetComponentsInChildren<Animator>(true);
        previousAnimatorUpdateModes = new AnimatorUpdateMode[wheelAnimators.Length];
        for (int i = 0; i < wheelAnimators.Length; i++)
        {
            previousAnimatorUpdateModes[i] = wheelAnimators[i].updateMode;
        }

        if (cursor != null)
        {
            cursorRectTransform = cursor.rectTransform;
            cursorCanvas = cursor.canvas;
        }

        if (cursorMoveBorder != null)
        {
            cursorMoveBorderRectTransform = cursorMoveBorder.rectTransform;
            cursorMoveBorderCanvas = cursorMoveBorder.canvas;
        }

        gravityImageOriginalScales = new Vector3[gravityImages.Count];
        for (int i = 0; i < gravityImages.Count; i++)
        {
            if (gravityImages[i] != null)
            {
                gravityImageOriginalScales[i] = gravityImages[i].rectTransform.localScale;
            }
        }

        SetVisible(false);
    }

    void Update()
    {
        InputManager input = InputManager.Instance;
        bool isVisible = input.TabHeld;
        SetVisible(isVisible);

        if (isVisible && cursorRectTransform != null && cursorMoveBorderRectTransform != null
            && cursorMoveBorderCanvas != null && Mouse.current != null)
        {
            Camera eventCamera = cursorMoveBorderCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : cursorMoveBorderCanvas.worldCamera;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    cursorMoveBorderRectTransform, Mouse.current.position.ReadValue(), eventCamera, out Vector2 pointerPosition))
            {
                Rect borderBounds = cursorMoveBorderRectTransform.rect;
                cursorRectTransform.GetWorldCorners(cursorCorners);
                Vector3 cursorCenter = cursorMoveBorderRectTransform.InverseTransformPoint(cursorRectTransform.position);
                float cursorRadius = 0f;

                foreach (Vector3 corner in cursorCorners)
                {
                    Vector3 localCorner = cursorMoveBorderRectTransform.InverseTransformPoint(corner) - cursorCenter;
                    cursorRadius = Mathf.Max(cursorRadius, new Vector2(localCorner.x, localCorner.y).magnitude);
                }

                float maxRadius = Mathf.Max(0f, Mathf.Min(borderBounds.width, borderBounds.height) * 0.5f - cursorRadius);
                Vector2 pointerOffset = pointerPosition - borderBounds.center;
                if (pointerOffset.sqrMagnitude > maxRadius * maxRadius)
                {
                    pointerOffset = pointerOffset.normalized * maxRadius;
                }

                cursorRectTransform.position = cursorMoveBorderRectTransform.TransformPoint(borderBounds.center + pointerOffset);
            }
        }

        int hoveredIndex = isVisible && cursorRectTransform != null && cursorCanvas != null && Mouse.current != null
            ? TrySelectGravityArea(RectTransformUtility.WorldToScreenPoint(
                cursorCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? cursorCanvas.worldCamera : null,
                cursorRectTransform.position))
            : -1;

        if (isVisible)
        {
            UpdateHoveredGravityArea(hoveredIndex);
        }
        else if (input.TabReleasedThisFrame)
        {
            selectedGravityIndex = hoveredGravityIndex;
            if (selectedGravityIndex >= 0 && selectedGravityIndex < gravityImages.Count
                && gravityImages[selectedGravityIndex] != null)
            {
                Debug.Log($"Gravity selected: {gravityImages[selectedGravityIndex].name} (direction index {selectedGravityIndex})");
                GravitySelected?.Invoke(selectedGravityIndex);
            }

            UpdateHoveredGravityArea(-1);
        }
        else
        {
            UpdateHoveredGravityArea(-1);
        }
    }

    private void SetVisible(bool isVisible)
    {
        canvasGroup.alpha = isVisible ? 1f : 0f;
        canvasGroup.interactable = isVisible;
        canvasGroup.blocksRaycasts = isVisible;

        if (isVisible && !gravityWheelOpen)
        {
            previousTimeScale = Time.timeScale;
            previousFixedDeltaTime = Time.fixedDeltaTime;
            Time.timeScale = wheelOpenTimeScale;

            float fixedDeltaScale = previousTimeScale > 0f
                ? wheelOpenTimeScale / previousTimeScale
                : wheelOpenTimeScale;
            Time.fixedDeltaTime = previousFixedDeltaTime * fixedDeltaScale;
            SetWheelAnimatorsUnscaled(true);
        }
        else if (!isVisible && gravityWheelOpen)
        {
            RestoreTimeScale();
            SetWheelAnimatorsUnscaled(false);
        }

        gravityWheelOpen = isVisible;
    }

    private void OnDisable()
    {
        if (gravityWheelOpen)
        {
            RestoreTimeScale();
            SetWheelAnimatorsUnscaled(false);
            gravityWheelOpen = false;
        }
    }

    private void RestoreTimeScale()
    {
        Time.timeScale = previousTimeScale;
        Time.fixedDeltaTime = previousFixedDeltaTime;
    }

    private void SetWheelAnimatorsUnscaled(bool unscaled)
    {
        for (int i = 0; i < wheelAnimators.Length; i++)
        {
            if (wheelAnimators[i] != null)
            {
                wheelAnimators[i].updateMode = unscaled
                    ? AnimatorUpdateMode.UnscaledTime
                    : previousAnimatorUpdateModes[i];
            }
        }
    }
    private int TrySelectGravityArea(Vector2 cursorScreenPosition)
    {
        int closestIndex = -1;
        float closestAngularDistance = float.PositiveInfinity;

        for (int i = 0; i < gravityImages.Count; i++)
        {
            Image gravityImage = gravityImages[i];
            if (gravityImage == null)
            {
                continue;
            }

            Canvas imageCanvas = gravityImage.canvas;
            Camera eventCamera = imageCanvas != null && imageCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? imageCanvas.worldCamera
                : null;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    gravityImage.rectTransform, cursorScreenPosition, eventCamera, out Vector2 localPosition))
            {
                continue;
            }

            float originAngle;
            switch (gravityImage.fillOrigin)
            {
                case 0:
                    originAngle = 270f;
                    break;
                case 1:
                    originAngle = 0f;
                    break;
                case 2:
                    originAngle = 90f;
                    break;
                case 3:
                    originAngle = 180f;
                    break;
                default:
                    originAngle = 0f;
                    break;
            }
            float cursorAngle = Mathf.Repeat(Mathf.Atan2(localPosition.y, localPosition.x) * Mathf.Rad2Deg, 360f);
            float angleFromOrigin = gravityImage.fillClockwise
                ? Mathf.Repeat(originAngle - cursorAngle, 360f)
                : Mathf.Repeat(cursorAngle - originAngle, 360f);
            float fillAngle = gravityImage.fillAmount * 360f;
            if (angleFromOrigin > fillAngle)
            {
                continue;
            }

            float angularDistance = Mathf.Abs(angleFromOrigin - fillAngle * 0.5f);
            if (angularDistance < closestAngularDistance)
            {
                closestIndex = i;
                closestAngularDistance = angularDistance;
            }
        }

        return closestIndex;
    }

    private void UpdateHoveredGravityArea(int index)
    {
        if (hoveredGravityIndex == index)
        {
            return;
        }

        if (hoveredGravityIndex >= 0 && hoveredGravityIndex < gravityImages.Count
            && gravityImages[hoveredGravityIndex] != null)
        {
            gravityImages[hoveredGravityIndex].rectTransform.localScale = gravityImageOriginalScales[hoveredGravityIndex];
        }

        hoveredGravityIndex = index;
        if (hoveredGravityIndex >= 0 && hoveredGravityIndex < gravityImages.Count
            && gravityImages[hoveredGravityIndex] != null)
        {
            gravityImages[hoveredGravityIndex].rectTransform.localScale =
                gravityImageOriginalScales[hoveredGravityIndex] * 1.1f;
        }
    }
}
