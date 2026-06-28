using Blot.Core.StateMachine;
using Blot.Gameplay.Events;

namespace Blot.Gameplay.States
{
    /// <summary>
    /// Terminal state. Fires the MatchEnded event then waits.
    /// The game resumes only when GameManager.RestartMatch() is called from the UI.
    /// </summary>
    public class MatchEndState : IGameState
    {
        public GameStateId StateId => GameStateId.MatchEnd;

        public void Enter(GameContext ctx) =>
            GameEvents.MatchEnded(ctx.ScoreManager.GetMatchWinner());

        public void Exit(GameContext ctx) { }
    }
}
