// ReleaseEchoChargeCommand
// Responsibility: Asks an EchoEmitter to fire at its current charge. Does nothing if it was not
// charging.
using Game.Core;

namespace Game.Echo
{
    public sealed class ReleaseEchoChargeCommand : ICommand
    {
        private readonly EchoEmitter emitter;

        public ReleaseEchoChargeCommand(EchoEmitter emitter)
        {
            this.emitter = emitter;
        }

        public void Execute()
        {
            if (emitter != null)
            {
                emitter.ReleaseCharge();
            }
        }
    }
}
