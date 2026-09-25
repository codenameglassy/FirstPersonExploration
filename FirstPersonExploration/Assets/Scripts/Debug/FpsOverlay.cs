// FpsOverlay.cs
// On-screen performance overlay for device testing. Shows average FPS, worst frame time,
// CPU main thread, present wait, render thread and GPU frame times, draw calls, SetPass calls,
// triangles, memory, battery, and a one-time device summary (GPU, graphics API, CPU, RAM, display).
// Builds its own screen-space canvas at runtime, so it needs no prefab, font asset, or TextMeshPro.
// Values refresh once per sampling window through cached strings, so this script does not
// allocate in its steady-state update path.
// The toggle key (desktop) or a multi-finger tap (touch) cycles Full, Compact, and Hidden.
// Place on an empty root GameObject in the first scene that loads.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class FpsOverlay : MonoBehaviour
{
    private const int ModeFull = 0;
    private const int ModeCompact = 1;
    private const int ModeHidden = 2;
    private const int ModeCount = 3;
    private const int CompactRowCount = 2;

    private const float Padding = 10f;
    private const float PanelMargin = 16f;
    private const float LabelWidth = 130f;
    private const float ValueWidth = 170f;
    private const float InfoWidth = 460f;
    private const float RowSpacing = 4f;
    private const long BytesPerMegabyte = 1024L * 1024L;

    private const string Off = "off";
    private const string NotAvailable = "n/a";
    private const string Placeholder = "--";

    private static readonly Color GoodColor = new Color(0.35f, 1f, 0.35f, 1f);
    private static readonly Color OkColor = new Color(1f, 0.85f, 0.2f, 1f);
    private static readonly Color BadColor = new Color(1f, 0.3f, 0.3f, 1f);
    private static readonly Color LabelColor = new Color(0.75f, 0.75f, 0.75f, 1f);
    private static readonly Color InfoColor = new Color(0.85f, 0.85f, 0.85f, 1f);

    // Indexed by (int)BatteryStatus: Unknown, Charging, Discharging, NotCharging, Full.
    private static readonly string[] BatteryStatusNames = { "", "charging", "discharging", "not charging", "full" };

    [Tooltip("Seconds of frames averaged into each readout.")]
    [SerializeField, Min(0.1f)] private float _sampleWindow = 0.5f;

    [Tooltip("If enabled, the overlay only appears in the Editor and in Development Builds.")]
    [SerializeField] private bool _developmentBuildsOnly = false;

    [Tooltip("Keep the overlay alive through scene loads.")]
    [SerializeField] private bool _persistAcrossScenes = true;

    [Tooltip("Font size in reference pixels (1280x720 reference resolution).")]
    [SerializeField, Min(8)] private int _fontSize = 18;

    [Tooltip("FPS at or above this value shows green.")]
    [SerializeField, Min(1)] private int _goodFps = 55;

    [Tooltip("FPS at or above this value (and below Good) shows yellow. Lower shows red.")]
    [SerializeField, Min(1)] private int _okFps = 30;

    [SerializeField, Range(0f, 1f)] private float _backgroundOpacity = 0.6f;

    [Tooltip("Keyboard key that cycles Full, Compact, and Hidden.")]
    [SerializeField] private KeyCode _toggleKey = KeyCode.F3;

    [Tooltip("Number of fingers touching at once that cycles Full, Compact, and Hidden.")]
    [SerializeField, Range(2, 5)] private int _toggleTouchCount = 3;

    // Prevents a duplicate overlay when the scene that owns it is reloaded.
    private static bool s_active;

    private readonly List<GameObject> _detailObjects = new List<GameObject>();

    private FrameTimingSampler _frameTiming;
    private ProfilerCounters _counters;

    private LazyStringCache _plainInts;
    private LazyStringCache _wholeMs;
    private LazyStringCache _tenthsMs;
    private LazyStringCache _megabytes;
    private LazyStringCache _kiloTriangles;
    private LazyStringCache _battery;

    private RectTransform _panel;
    private Text _fpsValue;
    private Text _worstValue;
    private Text _cpuMainValue;
    private Text _presentWaitValue;
    private Text _renderThreadValue;
    private Text _gpuValue;
    private Text _drawCallsValue;
    private Text _setPassValue;
    private Text _trianglesValue;
    private Text _systemMemoryValue;
    private Text _gcMemoryValue;
    private Text _batteryValue;

    private float _fullWidth;
    private float _fullHeight;
    private float _compactWidth;
    private float _compactHeight;
    private int _rowCount;

    private float _elapsed;
    private float _worstDelta;
    private int _frames;
    private int _mode = ModeFull;
    private int _lastTouchCount;
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

        _frameTiming = new FrameTimingSampler();
        _counters = new ProfilerCounters();

        BuildCaches();
        BuildUi();
        ApplyMode();
    }

    private void OnEnable()
    {
        _counters?.Start();
    }

    private void OnDisable()
    {
        _counters?.Stop();
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
        HandleToggleInput();

        float delta = Time.unscaledDeltaTime;
        _elapsed += delta;
        _frames++;

        if (delta > _worstDelta)
        {
            _worstDelta = delta;
        }

        _frameTiming.Tick();

        if (_elapsed < _sampleWindow)
        {
            return;
        }

        _frameTiming.CloseWindow();

        if (_mode != ModeHidden)
        {
            Refresh();
        }

        _elapsed = 0f;
        _frames = 0;
        _worstDelta = 0f;
    }

    private void HandleToggleInput()
    {
        int touchCount = Input.touchCount;
        bool touchToggle = touchCount == _toggleTouchCount && _lastTouchCount < _toggleTouchCount;
        _lastTouchCount = touchCount;

        if (touchToggle || Input.GetKeyDown(_toggleKey))
        {
            _mode = (_mode + 1) % ModeCount;
            ApplyMode();
        }
    }

    private void Refresh()
    {
        int fps = Mathf.RoundToInt(_frames / _elapsed);
        _fpsValue.text = _plainInts.Get(fps);
        _fpsValue.color = fps >= _goodFps ? GoodColor : fps >= _okFps ? OkColor : BadColor;
        _worstValue.text = _wholeMs.Get(Mathf.RoundToInt(_worstDelta * 1000f));

        if (_mode != ModeFull)
        {
            return;
        }

        SetCpuTime(_cpuMainValue, _frameTiming.AverageMainThreadMs);
        SetCpuTime(_presentWaitValue, _frameTiming.AveragePresentWaitMs);
        SetCpuTime(_renderThreadValue, _frameTiming.AverageRenderThreadMs);
        SetGpuTime();

        SetCount(_drawCallsValue, _counters.DrawCalls);
        SetCount(_setPassValue, _counters.SetPassCalls);
        SetTriangles(_counters.Triangles);
        SetMegabytes(_systemMemoryValue, _counters.SystemUsedBytes);
        SetMegabytes(_gcMemoryValue, _counters.GcUsedBytes);
        SetBattery();
    }

    private void SetCpuTime(Text target, double milliseconds)
    {
        if (!_frameTiming.IsEnabled)
        {
            target.text = Off;
            return;
        }

        target.text = _frameTiming.HasCpuData ? _tenthsMs.Get(ToTenths(milliseconds)) : NotAvailable;
    }

    private void SetGpuTime()
    {
        if (!_frameTiming.IsEnabled)
        {
            _gpuValue.text = Off;
            return;
        }

        _gpuValue.text = _frameTiming.HasGpuData ? _tenthsMs.Get(ToTenths(_frameTiming.AverageGpuMs)) : NotAvailable;
    }

    private void SetCount(Text target, long count)
    {
        target.text = count > 0L ? _plainInts.Get(ClampToInt(count)) : NotAvailable;
    }

    private void SetTriangles(long triangles)
    {
        _trianglesValue.text = triangles > 0L ? _kiloTriangles.Get(ClampToInt((triangles + 500L) / 1000L)) : NotAvailable;
    }

    private void SetMegabytes(Text target, long bytes)
    {
        target.text = bytes > 0L ? _megabytes.Get(ClampToInt(bytes / BytesPerMegabyte)) : NotAvailable;
    }

    private void SetBattery()
    {
        float level = SystemInfo.batteryLevel;
        if (level < 0f)
        {
            _batteryValue.text = NotAvailable;
            return;
        }

        int status = Mathf.Clamp((int)SystemInfo.batteryStatus, 0, BatteryStatusNames.Length - 1);
        int percent = Mathf.Clamp(Mathf.RoundToInt(level * 100f), 0, 100);
        _batteryValue.text = _battery.Get(percent * BatteryStatusNames.Length + status);
    }

    private void ApplyMode()
    {
        bool visible = _mode == ModeFull || _mode == ModeCompact;
        bool full = _mode == ModeFull;

        _panel.gameObject.SetActive(visible);

        for (int i = 0; i < _detailObjects.Count; i++)
        {
            _detailObjects[i].SetActive(full);
        }

        _panel.sizeDelta = full
            ? new Vector2(_fullWidth, _fullHeight)
            : new Vector2(_compactWidth, _compactHeight);
    }

    private void BuildCaches()
    {
        _plainInts = new LazyStringCache(10000, i => i.ToString());
        _wholeMs = new LazyStringCache(1000, i => i.ToString() + " ms");
        _tenthsMs = new LazyStringCache(10000, i => (i / 10).ToString() + "." + (i % 10).ToString() + " ms");
        _megabytes = new LazyStringCache(16384, i => i.ToString() + " MB");
        _kiloTriangles = new LazyStringCache(10000, i => i == 0 ? "<1k" : i.ToString() + "k");
        _battery = new LazyStringCache(101 * BatteryStatusNames.Length, FormatBattery);
    }

    private void BuildUi()
    {
        var canvasGo = new GameObject("PerformanceOverlayCanvas");
        canvasGo.transform.SetParent(transform, false);

        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        var panelGo = new GameObject("Panel", typeof(RectTransform));
        panelGo.transform.SetParent(canvasGo.transform, false);
        _panel = (RectTransform)panelGo.transform;
        SetTopLeft(_panel, new Vector2(PanelMargin, -PanelMargin), Vector2.zero);

        Image background = panelGo.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, _backgroundOpacity);
        background.raycastTarget = false;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        float rowHeight = _fontSize + RowSpacing;

        _fpsValue = CreateRow(font, "FPS", rowHeight);
        _worstValue = CreateRow(font, "Worst frame", rowHeight);
        _cpuMainValue = CreateRow(font, "CPU main", rowHeight);
        _presentWaitValue = CreateRow(font, "Present wait", rowHeight);
        _renderThreadValue = CreateRow(font, "Render thread", rowHeight);
        _gpuValue = CreateRow(font, "GPU", rowHeight);
        _drawCallsValue = CreateRow(font, "Draw calls", rowHeight);
        _setPassValue = CreateRow(font, "SetPass calls", rowHeight);
        _trianglesValue = CreateRow(font, "Triangles", rowHeight);
        _systemMemoryValue = CreateRow(font, "System memory", rowHeight);
        _gcMemoryValue = CreateRow(font, "GC memory", rowHeight);
        _batteryValue = CreateRow(font, "Battery", rowHeight);

        float infoTop = Padding + _rowCount * rowHeight + Padding;
        Text info = CreateText(_panel, "DeviceInfo", font, Mathf.Max(8, _fontSize - 2), InfoColor);
        info.horizontalOverflow = HorizontalWrapMode.Wrap;
        SetTopLeft(info.rectTransform, new Vector2(Padding, -infoTop), new Vector2(InfoWidth, 0f));
        info.text = DeviceInfoFormatter.Build();
        float infoHeight = info.preferredHeight;
        info.rectTransform.sizeDelta = new Vector2(InfoWidth, infoHeight);
        _detailObjects.Add(info.gameObject);

        _compactWidth = Padding * 2f + LabelWidth + ValueWidth;
        _compactHeight = Padding * 2f + CompactRowCount * rowHeight;
        _fullWidth = Padding * 2f + Mathf.Max(LabelWidth + ValueWidth, InfoWidth);
        _fullHeight = infoTop + infoHeight + Padding;
    }

    private Text CreateRow(Font font, string label, float rowHeight)
    {
        var rowGo = new GameObject(label, typeof(RectTransform));
        rowGo.transform.SetParent(_panel, false);
        SetTopLeft((RectTransform)rowGo.transform,
            new Vector2(Padding, -(Padding + _rowCount * rowHeight)),
            new Vector2(LabelWidth + ValueWidth, rowHeight));

        Text labelText = CreateText(rowGo.transform, "Label", font, _fontSize, LabelColor);
        SetTopLeft(labelText.rectTransform, Vector2.zero, new Vector2(LabelWidth, rowHeight));
        labelText.text = label;

        Text valueText = CreateText(rowGo.transform, "Value", font, _fontSize, Color.white);
        SetTopLeft(valueText.rectTransform, new Vector2(LabelWidth, 0f), new Vector2(ValueWidth, rowHeight));
        valueText.text = Placeholder;

        if (_rowCount >= CompactRowCount)
        {
            _detailObjects.Add(rowGo);
        }

        _rowCount++;
        return valueText;
    }

    private static Text CreateText(Transform parent, string objectName, Font font, int fontSize, Color color)
    {
        var go = new GameObject(objectName, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        Text text = go.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAnchor.UpperLeft;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    private static void SetTopLeft(RectTransform rect, Vector2 position, Vector2 size)
    {
        var topLeft = new Vector2(0f, 1f);
        rect.anchorMin = topLeft;
        rect.anchorMax = topLeft;
        rect.pivot = topLeft;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static int ToTenths(double milliseconds)
    {
        return Mathf.RoundToInt((float)(milliseconds * 10.0));
    }

    private static int ClampToInt(long value)
    {
        return value > int.MaxValue ? int.MaxValue : (int)value;
    }

    private static string FormatBattery(int key)
    {
        int count = BatteryStatusNames.Length;
        int percent = key / count;
        string status = BatteryStatusNames[key % count];
        return status.Length == 0 ? percent.ToString() + "%" : percent.ToString() + "% " + status;
    }
}
