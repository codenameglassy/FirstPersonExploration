// Lightweight reference to a playing sound. Becomes invalid once the sound finishes, is stopped,
// or its emitter is reused, so a stale handle can never stop someone else's sound.
public readonly struct AudioHandle
{
    public static readonly AudioHandle Invalid = default;

    private readonly AudioEmitter _emitter;
    private readonly int _generation;

    internal AudioHandle(AudioEmitter emitter, int generation)
    {
        _emitter = emitter;
        _generation = generation;
    }

    public bool IsValid => _emitter != null && _emitter.IsActive && _emitter.Generation == _generation;

    internal AudioEmitter Emitter => _emitter;
}
