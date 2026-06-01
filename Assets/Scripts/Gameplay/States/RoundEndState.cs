using Blot.Core.StateMachine;
using Blot.Gameplay.Events;

namespace Blot.Gameplay.States
{
    public class RoundEndState : IGameState
    {
        public GameStateId StateId => GameStateId.RoundEnd;

        public void Enter(GameContext ctx)
        {
            // Pass the bidding team so ScoreManager can apply the contract rule.
            var result = ctx.ScoreManager.FinalizeRound(ctx.RoundManager.BiddingTeam);
            GameEvents.RoundEnded(result);
            ctx.StateMachine.TransitionTo(GameStateId.CheckMatchEnd);
        }

        public void Exit(GameContext ctx) { }
    }
}
