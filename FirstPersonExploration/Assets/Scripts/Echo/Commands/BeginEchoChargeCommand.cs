// BeginEchoChargeCommand
// Responsibility: Asks an EchoEmitter to start charging. Charging begins as soon as the emitter is ready,
// so a press held through the cooldown is never lost.
using Game.Core;

namespace Game.Echo
{
    public sealed class BeginEchoChargeCommand : ICommand
    {
        private readonly EchoEmitter emitter;

        public BeginEchoChargeCommand(EchoEmitter emitter)
        {
            this.emitter = emitter;
        }

        public void Execute()
        {
            if (emitter != null)
            {
                emitter.BeginCharge();
            }
        }
    }
}
