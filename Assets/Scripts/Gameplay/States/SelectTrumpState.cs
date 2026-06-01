using Blot.Core.StateMachine;
using Blot.Gameplay.Events;

namespace Blot.Gameplay.States
{
    public class SelectTrumpState : IGameState
    {
        public GameStateId StateId => GameStateId.SelectTrump;

        public void Enter(GameContext ctx)
        {
            var trump      = ctx.MatchManager.SelectRandomTrump();
            int leadPlayer = ctx.MatchManager.SelectRandomLeadPlayer();

            ctx.RoundManager.StartRound(trump, leadPlayer);
            GameEvents.TrumpSelected(trump);
            ctx.StateMachine.TransitionTo(GameStateId.PlayTrick);
        }

        public void Exit(GameContext ctx) { }
    }
}
