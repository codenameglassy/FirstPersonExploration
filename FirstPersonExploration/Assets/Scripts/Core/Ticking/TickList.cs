// TickList<T>
// Responsibility: Allocation free list that is safe to modify while being iterated. Removals null
// the slot immediately so removed items are skipped in the same frame; Compact runs after the loop.
// Items added during iteration start ticking on the next frame.
using System.Collections.Generic;

namespace Game.Core
{
    public sealed class TickList<T> where T : class
    {
        private readonly List<T> items;
        private bool hasEmptySlots;

        public TickList(int capacity)
        {
            items = new List<T>(capacity);
        }

        public int Count => items.Count;

        public T this[int index] => items[index];

        public bool Add(T item)
        {
            if (item == null || items.Contains(item))
            {
                return false;
            }

            items.Add(item);
            return true;
        }

        public bool Remove(T item)
        {
            if (item == null)
            {
                return false;
            }

            int index = items.IndexOf(item);
            if (index < 0)
            {
                return false;
            }

            items[index] = null;
            hasEmptySlots = true;
            return true;
        }

        public void Compact()
        {
            if (!hasEmptySlots)
            {
                return;
            }

            int write = 0;
            for (int read = 0; read < items.Count; read++)
            {
                T item = items[read];
                if (item != null)
                {
                    items[write] = item;
                    write++;
                }
            }

            items.RemoveRange(write, items.Count - write);
            hasEmptySlots = false;
        }
    }
}
