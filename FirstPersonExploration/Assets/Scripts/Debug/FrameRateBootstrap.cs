// FrameRateBootstrap.cs
// Applies global frame pacing and screen power settings once at startup:
// target frame rate, vSync count, and screen sleep timeout.
// Place on an empty root GameObject in the first scene that loads.
using UnityEngine;

[DefaultExecutionOrder(-1000)]
public sealed class FrameRateBootstrap : MonoBehaviour
{
    [Tooltip("Frame rate the app aims for. -1 uses the platform default (30 on Android).")]
    [SerializeField, Range(-1, 240)] private int _targetFrameRate = 60;

    [Tooltip("0 = vSync off. Ignored on Android and iOS, where the OS always syncs to the display and Target Frame Rate is used instead.")]
    [SerializeField, Range(0, 4)] private int _vSyncCount = 0;

    [Tooltip("Keep the screen awake while the app runs. Useful for device testing.")]
    [SerializeField] private bool _preventScreenSleep = true;

    private void Awake()
    {
        QualitySettings.vSyncCount = _vSyncCount;
        Application.targetFrameRate = _targetFrameRate;
        Screen.sleepTimeout = _preventScreenSleep ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting;
    }
}
