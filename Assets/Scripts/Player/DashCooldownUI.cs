using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays a dash cooldown indicator and readiness icon on the screen canvas (HUD).
/// Uses direct component references, serialized properties, and singleton instance access
/// without performing costly scene-wide FindObject scans.
/// </summary>
public class DashCooldownUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DashAbility dashAbility;
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private Sprite dashIconSprite;
    [SerializeField] private Sprite dialSprite;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image cooldownFillImage;
    [SerializeField] private Text cooldownText;

    [Header("Visual Styling")]
    [SerializeField] private Color readyColor = new Color(0.1f, 0.95f, 1f, 0.95f);
    [SerializeField] private Color coolingColor = new Color(0.35f, 0.45f, 0.55f, 0.5f);
    [SerializeField] private Color overlayFillColor = new Color(0f, 0f, 0f, 0.7f);
    [SerializeField] private Color bgColor = new Color(0.06f, 0.08f, 0.12f, 0.85f);
    [SerializeField] private bool showSecondsText = true;

    private GameObject generatedContainer;
    private GameObject generatedCanvasObj;
    private bool wasReady = true;

    public void BindDashAbility(DashAbility ability)
    {
        dashAbility = ability;
    }

    public void BindTargetCanvas(Canvas canvas)
    {
        targetCanvas = canvas;
    }

    private DashAbility ResolveDashAbility()
    {
        if (dashAbility != null)
        {
            return dashAbility;
        }

        dashAbility = GetComponent<DashAbility>()
            ?? GetComponentInParent<DashAbility>()
            ?? DashAbility.Instance;

        return dashAbility;
    }

    private void Awake()
    {
        ResolveDashAbility();
        EnsureUIElements();
    }

    private void Start()
    {
        ResolveDashAbility();
        EnsureUIElements();
    }

    private void Update()
    {
        DashAbility ability = ResolveDashAbility();
        if (ability == null)
        {
            return;
        }

        if (iconImage == null || cooldownFillImage == null)
        {
            EnsureUIElements();
            if (iconImage == null || cooldownFillImage == null)
            {
                return;
            }
        }

        bool ready = ability.IsReady;
        float remaining = ability.CooldownRemaining;
        float normalized = ability.CooldownNormalized; // 1 = full cooldown, 0 = ready

        if (cooldownFillImage != null)
        {
            cooldownFillImage.fillAmount = normalized;
            cooldownFillImage.enabled = normalized > 0.001f;
        }

        if (iconImage != null)
        {
            iconImage.color = ready ? readyColor : coolingColor;
            if (ready && !wasReady)
            {
                iconImage.rectTransform.localScale = Vector3.one * 1.25f;
            }
            else
            {
                iconImage.rectTransform.localScale = Vector3.MoveTowards(
                    iconImage.rectTransform.localScale, Vector3.one, 2.5f * Time.deltaTime);
            }
        }

        if (cooldownText != null)
        {
            if (showSecondsText && remaining > 0.05f)
            {
                cooldownText.text = $"{remaining:0.0}s";
            }
            else
            {
                cooldownText.text = "DASH";
            }
        }

        wasReady = ready;
    }

    private void EnsureUIElements()
    {
        // If already wired in Inspector or created, nothing more needed
        if (iconImage != null && cooldownFillImage != null)
        {
            return;
        }

        // Check if assigned in Inspector or attached within a Canvas hierarchy
        if (targetCanvas == null)
        {
            targetCanvas = GetComponentInParent<Canvas>();
        }

        // If this component is not inside an existing Canvas, create a dedicated ScreenSpaceOverlay HUD Canvas
        if (targetCanvas == null)
        {
            generatedCanvasObj = new GameObject("DashHUD_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            targetCanvas = generatedCanvasObj.GetComponent<Canvas>();
            targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            targetCanvas.sortingOrder = 20;

            CanvasScaler scaler = generatedCanvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
        }

        // Build UI container attached directly under the Screen Space Canvas
        generatedContainer = new GameObject("DashIconContainer", typeof(RectTransform));
        generatedContainer.transform.SetParent(targetCanvas.transform, false);

        RectTransform containerRect = generatedContainer.GetComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(1f, 0f); // Bottom-Right anchor
        containerRect.anchorMax = new Vector2(1f, 0f);
        containerRect.pivot = new Vector2(1f, 0f);
        containerRect.anchoredPosition = new Vector2(-60f, 60f);
        containerRect.sizeDelta = new Vector2(80f, 80f);

        // Background Panel
        GameObject bgObj = new GameObject("DashBG", typeof(RectTransform), typeof(Image));
        bgObj.transform.SetParent(generatedContainer.transform, false);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        Image bgImage = bgObj.GetComponent<Image>();
        bgImage.color = bgColor;
        if (dialSprite != null)
        {
            bgImage.sprite = dialSprite;
        }

        // Dash Icon
        GameObject iconObj = new GameObject("DashIcon", typeof(RectTransform), typeof(Image));
        iconObj.transform.SetParent(generatedContainer.transform, false);
        RectTransform iRect = iconObj.GetComponent<RectTransform>();
        iRect.anchorMin = Vector2.zero;
        iRect.anchorMax = Vector2.one;
        iRect.offsetMin = new Vector2(16f, 16f);
        iRect.offsetMax = new Vector2(-16f, -16f);
        iconImage = iconObj.GetComponent<Image>();
        iconImage.color = readyColor;
        if (dashIconSprite != null)
        {
            iconImage.sprite = dashIconSprite;
        }

        // Cooldown Radial Overlay
        GameObject fillObj = new GameObject("DashCooldownFill", typeof(RectTransform), typeof(Image));
        fillObj.transform.SetParent(generatedContainer.transform, false);
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
        if (dialSprite != null)
        {
            cooldownFillImage.sprite = dialSprite;
        }

        // Text Label underneath the icon
        GameObject textObj = new GameObject("DashLabel", typeof(RectTransform), typeof(Text));
        textObj.transform.SetParent(generatedContainer.transform, false);
        RectTransform tRect = textObj.GetComponent<RectTransform>();
        tRect.anchorMin = new Vector2(0f, 0f);
        tRect.anchorMax = new Vector2(1f, 0f);
        tRect.pivot = new Vector2(0.5f, 1f);
        tRect.anchoredPosition = new Vector2(0f, -6f);
        tRect.sizeDelta = new Vector2(100f, 22f);
        cooldownText = textObj.GetComponent<Text>();
        cooldownText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
            ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        cooldownText.fontSize = 13;
        cooldownText.fontStyle = FontStyle.Bold;
        cooldownText.alignment = TextAnchor.MiddleCenter;
        cooldownText.color = Color.white;
        cooldownText.text = "DASH";
    }

    private void OnDestroy()
    {
        if (generatedContainer != null)
        {
            Destroy(generatedContainer);
        }
        if (generatedCanvasObj != null)
        {
            Destroy(generatedCanvasObj);
        }
    }
}