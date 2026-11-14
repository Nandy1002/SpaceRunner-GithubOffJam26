using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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

    /// <summary>
    /// Invoked when a cardinal gravity direction is chosen:
    /// 0 = Down (Normal), 1 = Left (Left Wall), 2 = Up (Ceiling), 3 = Right (Right Wall).
    /// </summary>
    public event Action<int> GravitySelected;

    public int SelectedGravityIndex => selectedGravityIndex;
    public float WheelOpenTimeScale => wheelOpenTimeScale;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        wheelAnimators = GetComponentsInChildren<Animator>(true);
        previousAnimatorUpdateModes = new AnimatorUpdateMode[wheelAnimators.Length];
        for (int i = 0; i < wheelAnimators.Length; i++)
        {
            if (wheelAnimators[i] != null)
            {
                previousAnimatorUpdateModes[i] = wheelAnimators[i].updateMode;
            }
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

    private void Update()
    {
        InputManager input = InputManager.Instance;
        if (input == null)
        {
            return;
        }

        bool isVisible = input.TabHeld;
        SetVisible(isVisible);

        int hoveredCardinalIndex = -1;

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

                // Determine cardinal direction from offset from center
                const float deadZoneRadius = 15f;
                if (pointerOffset.magnitude > deadZoneRadius)
                {
                    float angle = Mathf.Atan2(pointerOffset.y, pointerOffset.x) * Mathf.Rad2Deg;
                    hoveredCardinalIndex = AngleToCardinalIndex(angle);
                }
            }
        }

        if (isVisible)
        {
            UpdateHoveredGravityArea(hoveredCardinalIndex);
        }
        else if (input.TabReleasedThisFrame)
        {
            if (hoveredGravityIndex >= 0)
            {
                selectedGravityIndex = hoveredGravityIndex;
                string[] dirNames = { "DOWN", "LEFT", "UP", "RIGHT" };
                string nameStr = (selectedGravityIndex >= 0 && selectedGravityIndex < dirNames.Length)
                    ? dirNames[selectedGravityIndex]
                    : selectedGravityIndex.ToString();
                Debug.Log($"Gravity direction selected: {nameStr} (cardinal index {selectedGravityIndex})");
                GravitySelected?.Invoke(selectedGravityIndex);
            }

            UpdateHoveredGravityArea(-1);
        }
        else
        {
            UpdateHoveredGravityArea(-1);
        }
    }

    /// <summary>
    /// Maps screen angle (-180..180) to 4 cardinal directions:
    /// 0 = Down, 1 = Left, 2 = Up, 3 = Right.
    /// </summary>
    private static int AngleToCardinalIndex(float angle)
    {
        // Up: 45 to 135 deg
        if (angle >= 45f && angle < 135f)
        {
            return 2;
        }
        // Down: -135 to -45 deg
        if (angle >= -135f && angle < -45f)
        {
            return 0;
        }
        // Left: 135 to 180 or -180 to -135 deg
        if (angle >= 135f || angle < -135f)
        {
            return 1;
        }
        // Right: -45 to 45 deg
        return 3;
    }

    private void SetVisible(bool isVisible)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = isVisible ? 1f : 0f;
            canvasGroup.interactable = isVisible;
            canvasGroup.blocksRaycasts = isVisible;
        }

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

    private void OnDestroy()
    {
        if (gravityWheelOpen)
        {
            RestoreTimeScale();
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
        if (wheelAnimators == null) return;
        for (int i = 0; i < wheelAnimators.Length; i++)
        {
            if (wheelAnimators[i] != null && i < previousAnimatorUpdateModes.Length)
            {
                wheelAnimators[i].updateMode = unscaled
                    ? AnimatorUpdateMode.UnscaledTime
                    : previousAnimatorUpdateModes[i];
            }
        }
    }

    private int CardinalToVisualIndex(int cardinalIndex)
    {
        if (cardinalIndex < 0) return -1;
        if (gravityImages.Count == 4)
        {
            return cardinalIndex;
        }
        if (gravityImages.Count == 8)
        {
            // In 8-segment wheel: 0 = Down, 2 = Left, 4 = Up, 6 = Right
            switch (cardinalIndex)
            {
                case 0: return 0;
                case 1: return 2;
                case 2: return 4;
                case 3: return 6;
            }
        }
        return cardinalIndex < gravityImages.Count ? cardinalIndex : -1;
    }

    private void UpdateHoveredGravityArea(int cardinalIndex)
    {
        if (hoveredGravityIndex == cardinalIndex)
        {
            return;
        }

        int previousVisualIndex = CardinalToVisualIndex(hoveredGravityIndex);
        if (previousVisualIndex >= 0 && previousVisualIndex < gravityImages.Count
            && gravityImages[previousVisualIndex] != null
            && previousVisualIndex < gravityImageOriginalScales.Length)
        {
            gravityImages[previousVisualIndex].rectTransform.localScale = gravityImageOriginalScales[previousVisualIndex];
        }

        hoveredGravityIndex = cardinalIndex;

        int newVisualIndex = CardinalToVisualIndex(hoveredGravityIndex);
        if (newVisualIndex >= 0 && newVisualIndex < gravityImages.Count
            && gravityImages[newVisualIndex] != null
            && newVisualIndex < gravityImageOriginalScales.Length)
        {
            gravityImages[newVisualIndex].rectTransform.localScale =
                gravityImageOriginalScales[newVisualIndex] * 1.15f;
        }
    }
}