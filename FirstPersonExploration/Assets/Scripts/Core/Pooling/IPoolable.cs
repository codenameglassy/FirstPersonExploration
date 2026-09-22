// IPoolable
// Responsibility: Contract for components on a pooled object. OnSpawned must fully reset runtime
// state; OnDespawned must unsubscribe every listener and stop anything still running.
namespace Game.Core
{
    public interface IPoolable
    {
        void OnSpawned();
        void OnDespawned();
    }
}
