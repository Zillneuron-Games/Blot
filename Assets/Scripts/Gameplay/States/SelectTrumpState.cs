using Blot.Core.StateMachine;
using Blot.Gameplay.Events;

namespace Blot.Gameplay.States
{
    /// <summary>
    /// Legacy fallback state — no longer reached in the normal game flow.
    /// Kept registered in GameManager so the FSM dictionary doesn't throw
    /// if something transitions here by mistake.
    /// In the live flow: DealCards → Bidding (which calls RoundManager.StartRound correctly).
    /// </summary>
    public class SelectTrumpState : IGameState
    {
        public GameStateId StateId => GameStateId.SelectTrump;

        public void Enter(GameContext ctx)
        {
            // Fallback path: set a random Round Starter then pick a random trump.
            ctx.RoundManager.SetInitialRoundStarter(ctx.MatchManager.SelectRandomLeadPlayer());
            var trump = ctx.MatchManager.SelectRandomTrump();
            ctx.RoundManager.StartRound(trump);   // no bidder
            GameEvents.TrumpSelected(trump);
            ctx.StateMachine.TransitionTo(GameStateId.PlayTrick);
        }

        public void Exit(GameContext ctx) { }
    }
}
