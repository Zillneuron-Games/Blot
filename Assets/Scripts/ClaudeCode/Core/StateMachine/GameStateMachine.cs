using System.Collections.Generic;
using UnityEngine;

namespace Blot.Core.StateMachine
{
    public class GameStateMachine
    {
        private readonly Dictionary<GameStateId, IGameState> _states = new();
        private IGameState  _current;
        private GameContext _context;

        public GameStateId CurrentStateId => _current?.StateId ?? GameStateId.GameStart;

        public void Initialize(GameContext context) => _context = context;

        public void RegisterState(IGameState state) => _states[state.StateId] = state;

        public void Start(GameStateId initial) => TransitionTo(initial);

        public void TransitionTo(GameStateId next)
        {
            if (!_states.TryGetValue(next, out var nextState))
            {
                Debug.LogError($"[FSM] No state registered for {next}");
                return;
            }

            _current?.Exit(_context);
            _current = nextState;
            Debug.Log($"[FSM] → {next}");
            _current.Enter(_context);
        }
    }
}
