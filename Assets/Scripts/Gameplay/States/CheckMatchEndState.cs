using Blot.Core.StateMachine;

namespace Blot.Gameplay.States
{
    public class CheckMatchEndState : IGameState
    {
        public GameStateId StateId => GameStateId.CheckMatchEnd;

        public void Enter(GameContext ctx)
        {
            var next = ctx.ScoreManager.IsMatchOver()
                ? GameStateId.MatchEnd
                : GameStateId.DealCards;

            ctx.StateMachine.TransitionTo(next);
        }

        public void Exit(GameContext ctx) { }
    }
}
