// FrameRateBootstrap.cs
// Applies global frame pacing and screen power settings once at startup:
// target frame rate, vSync count, and screen sleep timeout.
// WebGL uses its own target frame rate, because the browser drives the frame loop there.
// Place on an empty root GameObject in the first scene that loads.
using UnityEngine;

[DefaultExecutionOrder(-1000)]
public sealed class FrameRateBootstrap : MonoBehaviour
{
    [Tooltip("Frame rate the app aims for on native platforms. -1 uses the platform default (30 on Android).")]
    [SerializeField, Range(-1, 240)] private int _targetFrameRate = 60;

    [Tooltip("Frame rate for WebGL builds. Keep at -1 so the browser paces frames to the display; any other value switches Unity to timer-based pacing, which is less smooth.")]
    [SerializeField, Range(-1, 240)] private int _webGLTargetFrameRate = -1;

    [Tooltip("0 = vSync off. Ignored on Android, iOS, and WebGL, where the platform always syncs to the display.")]
    [SerializeField, Range(0, 4)] private int _vSyncCount = 0;

    [Tooltip("Keep the screen awake while the app runs. Useful for device testing. No effect on WebGL.")]
    [SerializeField] private bool _preventScreenSleep = true;

    private void Awake()
    {
        bool isWebGL = Application.platform == RuntimePlatform.WebGLPlayer;

        QualitySettings.vSyncCount = _vSyncCount;
        Application.targetFrameRate = isWebGL ? _webGLTargetFrameRate : _targetFrameRate;
        Screen.sleepTimeout = _preventScreenSleep ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting;
    }
}