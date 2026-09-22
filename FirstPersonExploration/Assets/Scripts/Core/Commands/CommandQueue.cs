// CommandQueue
// Responsibility: Fixed capacity, allocation free FIFO of ICommand instances. Player input and AI
// both enqueue into an actor's queue and the actor flushes it once per tick, so both share one
// pipeline. Commands enqueued during a flush run on the next flush.
using UnityEngine;

namespace Game.Core
{
    public sealed class CommandQueue
    {
        private readonly ICommand[] buffer;
        private int head;
        private int count;

        public CommandQueue(int capacity)
        {
            buffer = new ICommand[Mathf.Max(1, capacity)];
        }

        public int Count => count;
        public int Capacity => buffer.Length;

        public bool Enqueue(ICommand command)
        {
            if (command == null)
            {
                return false;
            }

            if (count == buffer.Length)
            {
                Debug.LogWarning("CommandQueue: Capacity reached, command dropped.");
                return false;
            }

            int tail = (head + count) % buffer.Length;
            buffer[tail] = command;
            count++;
            return true;
        }

        public void ExecuteAll()
        {
            int pending = count;

            for (int i = 0; i < pending; i++)
            {
                if (count == 0)
                {
                    return;
                }

                ICommand command = buffer[head];
                buffer[head] = null;
                head = (head + 1) % buffer.Length;
                count--;

                if (command != null)
                {
                    command.Execute();
                }
            }
        }

        public void Clear()
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = null;
            }

            head = 0;
            count = 0;
        }
    }
}
