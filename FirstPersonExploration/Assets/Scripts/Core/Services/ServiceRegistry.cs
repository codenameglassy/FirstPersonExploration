// ServiceRegistry
// Responsibility: Explicit registry for the few application lifetime services, populated by the
// Bootstrapper at startup. Gameplay systems are never registered here. Statics reset on load so
// the registry stays clean when domain reload is disabled.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    public static class ServiceRegistry
    {
        private static readonly Dictionary<Type, object> Services = new Dictionary<Type, object>(16);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Services.Clear();
        }

        public static void Register<T>(T service) where T : class
        {
            if (service == null)
            {
                Debug.LogError($"ServiceRegistry: Cannot register a null {typeof(T).Name}.");
                return;
            }

            Services[typeof(T)] = service;
        }

        public static void Unregister<T>(T service) where T : class
        {
            Type key = typeof(T);
            if (Services.TryGetValue(key, out object existing) && ReferenceEquals(existing, service))
            {
                Services.Remove(key);
            }
        }

        public static bool TryGet<T>(out T service) where T : class
        {
            service = null;

            if (!Services.TryGetValue(typeof(T), out object found))
            {
                return false;
            }

            if (found is UnityEngine.Object unityObject && unityObject == null)
            {
                return false;
            }

            service = found as T;
            return service != null;
        }

        public static T Get<T>() where T : class
        {
            if (TryGet(out T service))
            {
                return service;
            }

            Debug.LogError($"ServiceRegistry: {typeof(T).Name} is not registered. Is a Bootstrapper in the scene?");
            return null;
        }
    }
}
