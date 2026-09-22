// ICommand
// Responsibility: One executable action. Implementations bind their receiver at construction and
// are reused, so issuing a command never allocates.
namespace Game.Core
{
    public interface ICommand
    {
        void Execute();
    }
}
