// ProfilerCounters.cs
// Owns ProfilerRecorder handles for memory and render statistics and exposes their latest values.
// Memory counters are designed to work in release players. Render counters (draw calls,
// SetPass calls, triangles) may read 0 outside the Editor and Development Builds.
// A value of 0 means "not available" to the caller.
using Unity.Profiling;

public sealed class ProfilerCounters
{
    private ProfilerRecorder _systemUsedMemory;
    private ProfilerRecorder _gcUsedMemory;
    private ProfilerRecorder _drawCalls;
    private ProfilerRecorder _setPassCalls;
    private ProfilerRecorder _triangles;
    private bool _isRunning;

    public long SystemUsedBytes => ReadLast(in _systemUsedMemory);
    public long GcUsedBytes => ReadLast(in _gcUsedMemory);
    public long DrawCalls => ReadLast(in _drawCalls);
    public long SetPassCalls => ReadLast(in _setPassCalls);
    public long Triangles => ReadLast(in _triangles);

    public void Start()
    {
        if (_isRunning)
        {
            return;
        }

        _systemUsedMemory = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "System Used Memory");
        _gcUsedMemory = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Used Memory");
        _drawCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
        _setPassCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
        _triangles = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
        _isRunning = true;
    }

    public void Stop()
    {
        if (!_isRunning)
        {
            return;
        }

        _systemUsedMemory.Dispose();
        _gcUsedMemory.Dispose();
        _drawCalls.Dispose();
        _setPassCalls.Dispose();
        _triangles.Dispose();
        _isRunning = false;
    }

    private static long ReadLast(in ProfilerRecorder recorder)
    {
        return recorder.Valid ? recorder.LastValue : 0L;
    }
}
