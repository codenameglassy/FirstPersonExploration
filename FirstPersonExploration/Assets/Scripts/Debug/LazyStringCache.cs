// LazyStringCache.cs
// Flyweight cache of formatted strings keyed by a non-negative integer.
// Each string is created the first time its key is requested and reused afterwards,
// so repeated UI updates stop allocating once the common values have been seen.
// Keys outside the range are clamped to the nearest valid key.
using System;

public sealed class LazyStringCache
{
    private readonly string[] _entries;
    private readonly Func<int, string> _formatter;

    public LazyStringCache(int capacity, Func<int, string> formatter)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        _entries = new string[capacity];
        _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
    }

    public string Get(int key)
    {
        if (key < 0)
        {
            key = 0;
        }
        else if (key >= _entries.Length)
        {
            key = _entries.Length - 1;
        }

        string entry = _entries[key];
        if (entry == null)
        {
            entry = _formatter(key);
            _entries[key] = entry;
        }

        return entry;
    }
}
