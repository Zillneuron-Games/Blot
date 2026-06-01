using Blot.Core.StateMachine;

namespace Blot.Gameplay.States
{
    public class GameStartState : IGameState
    {
        public GameStateId StateId => GameStateId.GameStart;

        public void Enter(GameContext ctx)
        {
            ctx.ScoreManager.ResetRoundScores();
            ctx.StateMachine.TransitionTo(GameStateId.DealCards);
        }

        public void Exit(GameContext ctx) { }
    }
}
