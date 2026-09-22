// StateTransition
// Responsibility: Immutable pairing of a target state and the condition that makes it legal.
using System;

namespace Game.Core
{
    public sealed class StateTransition
    {
        public StateTransition(IState to, Func<bool> condition)
        {
            To = to;
            Condition = condition;
        }

        public IState To { get; }
        public Func<bool> Condition { get; }
    }
}
