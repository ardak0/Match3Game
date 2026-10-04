using System;
using System.Collections.Generic;

namespace Match3.Game
{
    /// <summary>
    /// Holds the current game state and switches between states.
    /// States are registered once and looked up by their type, so a state only needs the machine
    /// (not the other states) to say "go to the idle state next".
    ///
    /// Order of a switch: old.Exit(), Current = new, StateChanged event, new.Enter().
    /// A state may call ChangeTo from inside Enter (as its last statement) to pass straight through.
    /// </summary>
    public sealed class GameStateMachine
    {
        private readonly Dictionary<Type, IGameState> _states = new Dictionary<Type, IGameState>();

        /// <summary>The active state, or null before the first ChangeTo and after Stop.</summary>
        public IGameState Current { get; private set; }

        /// <summary>Raised with (previous, next) before next.Enter(). previous is null for the first state.</summary>
        public event Action<IGameState, IGameState> StateChanged;

        public void Register(IGameState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));

            // Add throws if this type was registered before, which is a wiring mistake worth failing on.
            _states.Add(state.GetType(), state);
        }

        public void ChangeTo<T>() where T : class, IGameState
        {
            if (!_states.TryGetValue(typeof(T), out IGameState next))
            {
                throw new InvalidOperationException("No state of type " + typeof(T).Name + " is registered.");
            }

            IGameState previous = Current;
            previous?.Exit();
            Current = next;
            StateChanged?.Invoke(previous, next);
            next.Enter();
        }

        public bool IsIn<T>() where T : class, IGameState => Current is T;

        /// <summary>Leaves the current state without entering another one (used when the level is restarted or destroyed).</summary>
        public void Stop()
        {
            Current?.Exit();
            Current = null;
        }
    }
}
