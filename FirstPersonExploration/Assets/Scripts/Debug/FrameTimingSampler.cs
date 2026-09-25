// FrameTimingSampler.cs
// Accumulates FrameTimingManager data (CPU main thread, present wait, render thread, GPU)
// over a sampling window and exposes the window averages in milliseconds.
// Requires "Frame Timing Stats" in Player Settings. GPU time is only reported on platforms
// and graphics APIs that support it; HasGpuData stays false otherwise.
// Construct on the main thread (for example in Awake), never in a field initializer.
using UnityEngine;

public sealed class FrameTimingSampler
{
    private readonly FrameTiming[] _latest = new FrameTiming[1];

    private double _mainThreadSum;
    private double _presentWaitSum;
    private double _renderThreadSum;
    private double _gpuSum;
    private int _cpuSampleCount;
    private int _gpuSampleCount;

    public FrameTimingSampler()
    {
        IsEnabled = FrameTimingManager.IsFeatureEnabled();
    }

    public bool IsEnabled { get; }
    public bool HasCpuData { get; private set; }
    public bool HasGpuData { get; private set; }
    public double AverageMainThreadMs { get; private set; }
    public double AveragePresentWaitMs { get; private set; }
    public double AverageRenderThreadMs { get; private set; }
    public double AverageGpuMs { get; private set; }

    // Call once per frame.
    public void Tick()
    {
        if (!IsEnabled)
        {
            return;
        }

        FrameTimingManager.CaptureFrameTimings();
        if (FrameTimingManager.GetLatestTimings(1, _latest) == 0)
        {
            return;
        }

        FrameTiming timing = _latest[0];

        if (timing.cpuMainThreadFrameTime > 0.0)
        {
            _mainThreadSum += timing.cpuMainThreadFrameTime;
            _presentWaitSum += timing.cpuMainThreadPresentWaitTime;
            _renderThreadSum += timing.cpuRenderThreadFrameTime;
            _cpuSampleCount++;
        }

        if (timing.gpuFrameTime > 0.0)
        {
            _gpuSum += timing.gpuFrameTime;
            _gpuSampleCount++;
        }
    }

    // Call at the end of each sampling window to publish averages and start a new window.
    public void CloseWindow()
    {
        HasCpuData = _cpuSampleCount > 0;
        HasGpuData = _gpuSampleCount > 0;

        AverageMainThreadMs = HasCpuData ? _mainThreadSum / _cpuSampleCount : 0.0;
        AveragePresentWaitMs = HasCpuData ? _presentWaitSum / _cpuSampleCount : 0.0;
        AverageRenderThreadMs = HasCpuData ? _renderThreadSum / _cpuSampleCount : 0.0;
        AverageGpuMs = HasGpuData ? _gpuSum / _gpuSampleCount : 0.0;

        _mainThreadSum = 0.0;
        _presentWaitSum = 0.0;
        _renderThreadSum = 0.0;
        _gpuSum = 0.0;
        _cpuSampleCount = 0;
        _gpuSampleCount = 0;
    }
}
