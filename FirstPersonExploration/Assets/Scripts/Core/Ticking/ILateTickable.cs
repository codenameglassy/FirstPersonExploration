// ILateTickable
// Responsibility: Late frame contract (camera follow, look) driven by TickManager instead of
// MonoBehaviour.LateUpdate.
namespace Game.Core
{
    public interface ILateTickable
    {
        void LateTick(float deltaTime);
    }
}
