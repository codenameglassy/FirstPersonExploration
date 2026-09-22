// TickManager
// Responsibility: Single Update, FixedUpdate and LateUpdate driver for every registered ITickable,
// IFixedTickable and ILateTickable. An exception in one tickable is logged and does not stop the rest.
using System;
using UnityEngine;

namespace Game.Core
{
    [DisallowMultipleComponent]
    public sealed class TickManager : MonoBehaviour
    {
        private readonly TickList<ITickable> tickables = new TickList<ITickable>(128);
        private readonly TickList<IFixedTickable> fixedTickables = new TickList<IFixedTickable>(64);
        private readonly TickList<ILateTickable> lateTickables = new TickList<ILateTickable>(32);

        public void Register(ITickable tickable)
        {
            tickables.Add(tickable);
        }

        public void Unregister(ITickable tickable)
        {
            tickables.Remove(tickable);
        }

        public void RegisterFixed(IFixedTickable tickable)
        {
            fixedTickables.Add(tickable);
        }

        public void UnregisterFixed(IFixedTickable tickable)
        {
            fixedTickables.Remove(tickable);
        }

        public void RegisterLate(ILateTickable tickable)
        {
            lateTickables.Add(tickable);
        }

        public void UnregisterLate(ILateTickable tickable)
        {
            lateTickables.Remove(tickable);
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            int count = tickables.Count;

            for (int i = 0; i < count; i++)
            {
                ITickable tickable = tickables[i];
                if (tickable == null)
                {
                    continue;
                }

                try
                {
                    tickable.Tick(deltaTime);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, tickable as UnityEngine.Object);
                }
            }

            tickables.Compact();
        }

        private void FixedUpdate()
        {
            float fixedDeltaTime = Time.fixedDeltaTime;
            int count = fixedTickables.Count;

            for (int i = 0; i < count; i++)
            {
                IFixedTickable tickable = fixedTickables[i];
                if (tickable == null)
                {
                    continue;
                }

                try
                {
                    tickable.FixedTick(fixedDeltaTime);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, tickable as UnityEngine.Object);
                }
            }

            fixedTickables.Compact();
        }

        private void LateUpdate()
        {
            float deltaTime = Time.deltaTime;
            int count = lateTickables.Count;

            for (int i = 0; i < count; i++)
            {
                ILateTickable tickable = lateTickables[i];
                if (tickable == null)
                {
                    continue;
                }

                try
                {
                    tickable.LateTick(deltaTime);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, tickable as UnityEngine.Object);
                }
            }

            lateTickables.Compact();
        }
    }
}
