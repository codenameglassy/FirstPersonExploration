// StateMachine
// Responsibility: Plain C# finite state machine. Transitions are declared up front; each Tick checks
// "any state" transitions first, then the current state's transitions, then ticks the current state.
// SetState is for the initial state or an explicit forced reset.
using System;
using System.Collections.Generic;

namespace Game.Core
{
    public sealed class StateMachine
    {
        private static readonly List<StateTransition> NoTransitions = new List<StateTransition>(0);

        private readonly Dictionary<IState, List<StateTransition>> transitionsByState =
            new Dictionary<IState, List<StateTransition>>(8);
        private readonly List<StateTransition> anyTransitions = new List<StateTransition>(4);
        private List<StateTransition> currentTransitions = NoTransitions;

        public IState Current { get; private set; }

        public event Action<IState, IState> StateChanged;

        public void AddTransition(IState from, IState to, Func<bool> condition)
        {
            if (from == null) throw new ArgumentNullException(nameof(from));
            if (to == null) throw new ArgumentNullException(nameof(to));
            if (condition == null) throw new ArgumentNullException(nameof(condition));

            if (!transitionsByState.TryGetValue(from, out List<StateTransition> list))
            {
                list = new List<StateTransition>(4);
                transitionsByState.Add(from, list);
            }

            list.Add(new StateTransition(to, condition));

            if (from == Current)
            {
                currentTransitions = list;
            }
        }

        public void AddAnyTransition(IState to, Func<bool> condition)
        {
            if (to == null) throw new ArgumentNullException(nameof(to));
            if (condition == null) throw new ArgumentNullException(nameof(condition));

            anyTransitions.Add(new StateTransition(to, condition));
        }

        public void SetState(IState state)
        {
            if (state == null || state == Current)
            {
                return;
            }

            IState previous = Current;
            if (previous != null)
            {
                previous.Exit();
            }

            Current = state;
            currentTransitions = transitionsByState.TryGetValue(state, out List<StateTransition> list)
                ? list
                : NoTransitions;

            Current.Enter();
            StateChanged?.Invoke(previous, Current);
        }

        public void Tick(float deltaTime)
        {
            StateTransition transition = FindTransition();
            if (transition != null)
            {
                SetState(transition.To);
            }

            if (Current != null)
            {
                Current.Tick(deltaTime);
            }
        }

        private StateTransition FindTransition()
        {
            for (int i = 0; i < anyTransitions.Count; i++)
            {
                StateTransition transition = anyTransitions[i];
                if (transition.To != Current && transition.Condition())
                {
                    return transition;
                }
            }

            for (int i = 0; i < currentTransitions.Count; i++)
            {
                StateTransition transition = currentTransitions[i];
                if (transition.Condition())
                {
                    return transition;
                }
            }

            return null;
        }
    }
}
