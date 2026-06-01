using Blot.Core.StateMachine;
using Blot.Gameplay.Events;

namespace Blot.Gameplay.States
{
    public class DealCardsState : IGameState
    {
        public GameStateId StateId => GameStateId.DealCards;

        public void Enter(GameContext ctx)
        {
            ctx.MatchManager.CreateAndShuffleDeck();
            ctx.MatchManager.DealCardsToPlayers();
            ctx.ScoreManager.ResetRoundScores();

            GameEvents.CardsDealt();
            ctx.StateMachine.TransitionTo(GameStateId.Bidding);
        }

        public void Exit(GameContext ctx) { }
    }
}
