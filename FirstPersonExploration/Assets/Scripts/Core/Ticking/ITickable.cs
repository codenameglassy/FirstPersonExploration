// ITickable
// Responsibility: Per frame update contract driven by TickManager instead of MonoBehaviour.Update.
namespace Game.Core
{
    public interface ITickable
    {
        void Tick(float deltaTime);
    }
}
