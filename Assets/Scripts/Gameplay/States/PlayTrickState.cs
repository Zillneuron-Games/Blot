using System.Collections.Generic;
using Blot.Cards;
using Blot.Core.StateMachine;
using Blot.Gameplay.Events;
using Blot.Gameplay.Rules;
using Blot.Players;

namespace Blot.Gameplay.States
{
    /// <summary>
    /// Drives one full trick: requests a card from each player in turn order.
    /// AI responds synchronously; HumanPlayer defers until a UI click fires OnCardChosen.
    /// Advances to EvaluateTrick once all four cards have been played.
    ///
    /// Also handles Belote / Rebelote detection per card played.
    /// </summary>
    public class PlayTrickState : IGameState
    {
        public GameStateId StateId => GameStateId.PlayTrick;

        private GameContext  _ctx;
        private List<Player> _turnOrder;
        private int          _turnIndex;

        public void Enter(GameContext ctx)
        {
            _ctx       = ctx;
            _turnIndex = 0;
            _turnOrder = ctx.RoundManager.GetTrickTurnOrder();
            RequestCurrentPlayerTurn();
        }

        private void RequestCurrentPlayerTurn()
        {
            var player = _turnOrder[_turnIndex];
            player.OnCardChosen += HandleCardChosen;
            GameEvents.PlayerTurnStarted(player);
            player.RequestPlay(
                _ctx.RoundManager.CurrentTrick,
                _ctx.RoundManager.Trump,
                _ctx.RoundManager.Players);
        }

        private void HandleCardChosen(Card card)
        {
            var player = _turnOrder[_turnIndex];
            player.OnCardChosen -= HandleCardChosen;   // unsubscribe immediately

            _ctx.RoundManager.CurrentTrick.AddPlay(player, card);

            // ---- Belote / Rebelote detection --------------------------------
            var beloteTracker = _ctx.RoundManager.BeloteTracker;
            if (beloteTracker != null)
            {
                var evt = beloteTracker.NotifyCardPlayed(player, card);
                if (evt == BeloteEvent.Rebelote)
                {
                    // Award the 20-point bonus immediately so it's included in FinalizeRound
                    _ctx.ScoreManager.AddTrickPoints(player.Team, 20);
                    GameEvents.BeloteAnnounced(player, BeloteEvent.Rebelote);
                }
                else if (evt == BeloteEvent.Belote)
                {
                    GameEvents.BeloteAnnounced(player, BeloteEvent.Belote);
                    // No points yet — waiting for Rebelote
                }
            }

            GameEvents.CardPlayed(player, card);

            _turnIndex++;
            if (_turnIndex >= 4)
                _ctx.StateMachine.TransitionTo(GameStateId.EvaluateTrick);
            else
                RequestCurrentPlayerTurn();
        }

        public void Exit(GameContext ctx) { }
    }
}
