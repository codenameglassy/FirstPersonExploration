// CancelEchoChargeCommand
// Responsibility: Asks an EchoEmitter to drop its charge without firing, for example when the cursor
// unlocks mid charge.
using Game.Core;

namespace Game.Echo
{
    public sealed class CancelEchoChargeCommand : ICommand
    {
        private readonly EchoEmitter emitter;

        public CancelEchoChargeCommand(EchoEmitter emitter)
        {
            this.emitter = emitter;
        }

        public void Execute()
        {
            if (emitter != null)
            {
                emitter.CancelCharge();
            }
        }
    }
}
