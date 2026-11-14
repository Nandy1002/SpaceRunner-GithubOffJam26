using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays a dash cooldown indicator and readiness icon on the player's HUD.
/// Works either with serialized UI references or automatically generates a clean HUD element at runtime.
/// </summary>
public class DashCooldownUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DashAbility dashAbility;
        [SerializeField] private Image iconImage;
        [SerializeField] private Image cooldownFillImage;
        [SerializeField] private Text cooldownText;

        [Header("Visual Styling")]
        [SerializeField] private Color readyColor = new Color(0.1f, 0.9f, 1f, 0.95f);
        [SerializeField] private Color coolingColor = new Color(0.3f, 0.4f, 0.5f, 0.5f);
        [SerializeField] private Color overlayFillColor = new Color(0f, 0f, 0f, 0.65f);
        [SerializeField] private bool showSecondsText = true;

        private CanvasGroup canvasGroup;
        private bool wasReady = true;

        private void Awake()
        {
            if (dashAbility == null)
            {
                dashAbility = FindFirstObjectByType<DashAbility>();
            }

            canvasGroup = GetComponent<CanvasGroup>();
            EnsureUIElements();
        }

        private void Start()
        {
            if (dashAbility == null)
            {
                dashAbility = FindFirstObjectByType<DashAbility>();
            }
        }

        private void Update()
        {
            if (dashAbility == null)
            {
                dashAbility = FindFirstObjectByType<DashAbility>();
                if (dashAbility == null)
                {
                    return;
                }
            }

            bool ready = dashAbility.IsReady;
            float remaining = dashAbility.CooldownRemaining;
            float normalized = dashAbility.CooldownNormalized; // 1 = full cooldown remaining, 0 = ready

            if (cooldownFillImage != null)
            {
                cooldownFillImage.fillAmount = normalized;
                cooldownFillImage.enabled = normalized > 0.001f;
            }

            if (iconImage != null)
            {
                iconImage.color = ready ? readyColor : coolingColor;
                // Subtle scale pulse when ready transition occurs
                if (ready && !wasReady)
                {
                    iconImage.rectTransform.localScale = Vector3.one * 1.25f;
                }
                else
                {
                    iconImage.rectTransform.localScale = Vector3.MoveTowards(
                        iconImage.rectTransform.localScale, Vector3.one, 2f * Time.deltaTime);
                }
            }

            if (cooldownText != null)
            {
                if (showSecondsText && remaining > 0.05f)
                {
                    cooldownText.text = $"{remaining:0.0}s";
                    cooldownText.enabled = true;
                }
                else
                {
                    cooldownText.text = "DASH";
                    cooldownText.enabled = true;
                }
            }

            wasReady = ready;
        }

        private void EnsureUIElements()
        {
            // If already wired in Inspector, nothing more needed
            if (iconImage != null && cooldownFillImage != null)
            {
                return;
            }

            // If this component is attached to an existing Canvas / HUD root, create a sub-panel
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                // Create a standalone HUD canvas if none exists
                GameObject canvasObj = new GameObject("DashHUD_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasObj.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 10;
                CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                transform.SetParent(canvasObj.transform, false);
            }

            // Create Icon Container in bottom-right corner
            GameObject container = new GameObject("DashIconContainer", typeof(RectTransform));
            container.transform.SetParent(transform, false);
            RectTransform containerRect = container.GetComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(1f, 0f);
            containerRect.anchorMax = new Vector2(1f, 0f);
            containerRect.pivot = new Vector2(1f, 0f);
            containerRect.anchoredPosition = new Vector2(-60f, 60f);
            containerRect.sizeDelta = new Vector2(90f, 90f);

            // Background / Dial
            GameObject bgObj = new GameObject("DashBG", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(container.transform, false);
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            Image bgImage = bgObj.GetComponent<Image>();
            bgImage.color = new Color(0.08f, 0.12f, 0.18f, 0.8f);

            // Icon Image
            GameObject iconObj = new GameObject("DashIcon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(container.transform, false);
            RectTransform iRect = iconObj.GetComponent<RectTransform>();
            iRect.anchorMin = Vector2.zero;
            iRect.anchorMax = Vector2.one;
            iRect.offsetMin = new Vector2(14f, 14f);
            iRect.offsetMax = new Vector2(-14f, -14f);
            iconImage = iconObj.GetComponent<Image>();
            iconImage.color = readyColor;

            // Load sprite if available
            Sprite sprite = Resources.Load<Sprite>("Sprites/triangle")
                ?? Resources.Load<Sprite>("triangle");
            if (sprite != null)
            {
                iconImage.sprite = sprite;
            }

            // Cooldown Radial Fill Overlay
            GameObject fillObj = new GameObject("DashCooldownFill", typeof(RectTransform), typeof(Image));
            fillObj.transform.SetParent(container.transform, false);
            RectTransform fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            cooldownFillImage = fillObj.GetComponent<Image>();
            cooldownFillImage.color = overlayFillColor;
            cooldownFillImage.type = Image.Type.Filled;
            cooldownFillImage.fillMethod = Image.FillMethod.Radial360;
            cooldownFillImage.fillOrigin = (int)Image.Origin360.Top;
            cooldownFillImage.fillClockwise = false;
            cooldownFillImage.fillAmount = 0f;

            // Label Text
            GameObject textObj = new GameObject("DashLabel", typeof(RectTransform), typeof(Text));
            textObj.transform.SetParent(container.transform, false);
            RectTransform tRect = textObj.GetComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0f, 0f);
            tRect.anchorMax = new Vector2(1f, 0f);
            tRect.pivot = new Vector2(0.5f, 1f);
            tRect.anchoredPosition = new Vector2(0f, -6f);
            tRect.sizeDelta = new Vector2(120f, 24f);
            cooldownText = textObj.GetComponent<Text>();
            cooldownText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            cooldownText.fontSize = 14;
            cooldownText.alignment = TextAnchor.MiddleCenter;
            cooldownText.color = Color.white;
            cooldownText.text = "DASH";
        }
    }