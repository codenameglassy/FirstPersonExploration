// IFixedTickable
// Responsibility: Physics step contract driven by TickManager instead of MonoBehaviour.FixedUpdate.
namespace Game.Core
{
    public interface IFixedTickable
    {
        void FixedTick(float fixedDeltaTime);
    }
}
