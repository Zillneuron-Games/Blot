using System.Collections.Generic;
using Blot.Cards;
using Blot.Core.StateMachine;
using Blot.Gameplay.Events;
using Blot.Gameplay.Rules;
using Blot.Players;
using UnityEngine;

namespace Blot.Gameplay.States
{
    /// <summary>
    /// Drives one full trick (4 cards).
    ///
    /// Turn order:
    ///   Starts with the Trick Leader (RoundManager.CurrentTrickLeader) and
    ///   proceeds clockwise.  AI responds synchronously; HumanPlayer defers
    ///   until a UI click fires OnCardChosen.
    ///
    /// Transitions to EvaluateTrick once all four cards have been played.
    /// Also detects Belote / Rebelote and awards the bonus immediately.
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

            Debug.Log($"[Trick {ctx.RoundManager.CurrentTrickIndex}] " +
                      $"Leader = Player{_turnOrder[0].Id} ({_turnOrder[0].Name}) | " +
                      $"Round {ctx.RoundManager.CurrentRoundIndex}");

            RequestCurrentPlayerTurn();
        }

        // ------------------------------------------------------------------ turn loop

        private void RequestCurrentPlayerTurn()
        {
            var player = _turnOrder[_turnIndex];

            _ctx.RoundManager.SetActivePlayer(player);
            Debug.Log($"[Turn] Active Player = Player{player.Id} ({player.Name})");

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
            _ctx.RoundManager.NotifyCardPlayed();

            Debug.Log($"[Card Played] Player{player.Id} ({player.Name}) → {card} " +
                      $"({_ctx.RoundManager.CardsPlayedThisTrick}/4 this trick)");

            // ---- Belote / Rebelote detection --------------------------------
            var beloteTracker = _ctx.RoundManager.BeloteTracker;
            if (beloteTracker != null)
            {
                var evt = beloteTracker.NotifyCardPlayed(player, card);
                if (evt == BeloteEvent.Rebelote)
                {
                    _ctx.ScoreManager.AddTrickPoints(player.Team, 20);
                    Debug.Log($"[Rebelote] Player{player.Id} ({player.Name}) completes Belote/Rebelote! +20 pts for {player.Team}");
                    GameEvents.BeloteAnnounced(player, BeloteEvent.Rebelote);
                }
                else if (evt == BeloteEvent.Belote)
                {
                    Debug.Log($"[Belote] Player{player.Id} ({player.Name}) announces Belote!");
                    GameEvents.BeloteAnnounced(player, BeloteEvent.Belote);
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
