// FpsOverlay.cs
// On-screen debug readout of average FPS and worst frame time over a sampling window.
// Builds its own screen-space overlay canvas at runtime, so it needs no prefab, font asset,
// or TextMeshPro setup. Labels are precomputed once, so the per-frame path does not allocate.
// Place on an empty root GameObject in the first scene that loads.
using UnityEngine;
using UnityEngine.UI;

public sealed class FpsOverlay : MonoBehaviour
{
    private const int MaxDisplayedFps = 240;
    private const int MaxDisplayedMs = 999;
    private const float TopPadding = 16f;
    private const float LineSpacing = 6f;
    private const float LabelWidth = 400f;

    private static readonly Color GoodColor = new Color(0.35f, 1f, 0.35f, 1f);
    private static readonly Color OkColor = new Color(1f, 0.85f, 0.2f, 1f);
    private static readonly Color BadColor = new Color(1f, 0.3f, 0.3f, 1f);

    [Tooltip("Seconds of frames averaged into each readout.")]
    [SerializeField, Min(0.1f)] private float _sampleWindow = 0.5f;

    [Tooltip("If enabled, the overlay only appears in the Editor and in Development Builds.")]
    [SerializeField] private bool _developmentBuildsOnly = false;

    [Tooltip("Keep the overlay alive through scene loads.")]
    [SerializeField] private bool _persistAcrossScenes = true;

    [SerializeField, Min(8)] private int _fontSize = 28;

    [Tooltip("FPS at or above this value shows green.")]
    [SerializeField, Min(1)] private int _goodFps = 55;

    [Tooltip("FPS at or above this value (and below Good) shows yellow. Lower shows red.")]
    [SerializeField, Min(1)] private int _okFps = 30;

    // Prevents a duplicate overlay when the scene that owns it is reloaded.
    private static bool s_active;
    private static string[] s_fpsLabels;
    private static string[] s_msLabels;

    private Text _fpsText;
    private Text _msText;
    private float _elapsed;
    private float _worstDelta;
    private int _frames;
    private bool _isOwner;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        s_active = false;
    }

    private void Awake()
    {
        if (s_active || (_developmentBuildsOnly && !Debug.isDebugBuild))
        {
            enabled = false;
            Destroy(this);
            return;
        }

        s_active = true;
        _isOwner = true;

        if (_persistAcrossScenes)
        {
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }

        BuildLabelCache();
        BuildUi();
    }

    private void OnDestroy()
    {
        if (_isOwner)
        {
            s_active = false;
        }
    }

    private void Update()
    {
        float delta = Time.unscaledDeltaTime;
        _elapsed += delta;
        _frames++;

        if (delta > _worstDelta)
        {
            _worstDelta = delta;
        }

        if (_elapsed < _sampleWindow)
        {
            return;
        }

        int fps = Mathf.Clamp(Mathf.RoundToInt(_frames / _elapsed), 0, MaxDisplayedFps);
        int worstMs = Mathf.Clamp(Mathf.RoundToInt(_worstDelta * 1000f), 0, MaxDisplayedMs);

        _fpsText.text = s_fpsLabels[fps];
        _fpsText.color = fps >= _goodFps ? GoodColor : fps >= _okFps ? OkColor : BadColor;
        _msText.text = s_msLabels[worstMs];

        _elapsed = 0f;
        _frames = 0;
        _worstDelta = 0f;
    }

    private static void BuildLabelCache()
    {
        if (s_fpsLabels != null)
        {
            return;
        }

        s_fpsLabels = new string[MaxDisplayedFps + 1];
        for (int i = 0; i <= MaxDisplayedFps; i++)
        {
            s_fpsLabels[i] = i.ToString() + " FPS";
        }

        s_msLabels = new string[MaxDisplayedMs + 1];
        for (int i = 0; i <= MaxDisplayedMs; i++)
        {
            s_msLabels[i] = "worst " + i.ToString() + " ms";
        }
    }

    private void BuildUi()
    {
        var canvasGo = new GameObject("FpsOverlayCanvas");
        canvasGo.transform.SetParent(transform, false);

        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        _fpsText = CreateLabel(canvasGo.transform, "FpsLabel", font, 0f);
        _msText = CreateLabel(canvasGo.transform, "WorstFrameLabel", font, _fontSize + LineSpacing);

        _fpsText.text = "-- FPS";
        _msText.text = "worst -- ms";
    }

    private Text CreateLabel(Transform parent, string labelName, Font font, float yOffset)
    {
        var go = new GameObject(labelName, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        // Top center stays clear of camera notches in landscape.
        var rect = (RectTransform)go.transform;
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -(TopPadding + yOffset));
        rect.sizeDelta = new Vector2(LabelWidth, _fontSize + LineSpacing);

        Text text = go.AddComponent<Text>();
        text.font = font;
        text.fontSize = _fontSize;
        text.alignment = TextAnchor.UpperCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        text.color = Color.white;

        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2f, -2f);

        return text;
    }
}
