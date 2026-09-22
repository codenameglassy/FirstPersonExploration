// IState
// Responsibility: Contract for one discrete mode in a StateMachine. Each state is its own class.
namespace Game.Core
{
    public interface IState
    {
        void Enter();
        void Tick(float deltaTime);
        void Exit();
    }
}
