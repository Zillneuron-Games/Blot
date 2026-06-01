using System;
using System.Collections.Generic;
using Blot.Cards;
using Blot.Core.StateMachine;
using Blot.Gameplay.Events;
using Blot.Players;

namespace Blot.Gameplay.States
{
    /// <summary>
    /// Drives the bidding phase that replaces the old random trump selection.
    ///
    /// Flow:
    ///   • Players bid in turn order starting from player 0.
    ///   • The first player to name a suit becomes the bidder; that suit = trump.
    ///   • If all four players pass the round is redealt (→ DealCards).
    ///
    /// AI bids synchronously. HumanPlayer fires OnBidRequested and waits for
    /// TryPlaceBid() to be called (by UI or auto-fallback).
    /// </summary>
    public class BiddingState : IGameState
    {
        public GameStateId StateId => GameStateId.Bidding;

        private GameContext  _ctx;
        private List<Player> _order;    // 4 players in bidding order
        private int          _bidIndex;

        public void Enter(GameContext ctx)
        {
            _ctx      = ctx;
            _bidIndex = 0;

            // Bidding order: player 0 first (fixed for MVP; can rotate per round later)
            _order = new List<Player>(_ctx.MatchManager.Players);

            RequestCurrentBidderTurn();
        }

        // ------------------------------------------------------------------ turn loop

        private void RequestCurrentBidderTurn()
        {
            var player = _order[_bidIndex];
            player.OnBidChosen += HandleBidChosen;
            GameEvents.BidRequested(player);
            player.RequestBid();
        }

        private void HandleBidChosen(Suit? bid)
        {
            var player = _order[_bidIndex];
            player.OnBidChosen -= HandleBidChosen;   // unsubscribe immediately

            GameEvents.BidPlaced(player, bid);

            if (bid.HasValue)
            {
                // A suit was named — bidding is over
                Suit trump     = bid.Value;
                int  leadIndex = Array.IndexOf(_ctx.MatchManager.Players, player);

                _ctx.RoundManager.StartRound(trump, leadIndex, player);
                GameEvents.TrumpSelected(trump);           // reuse existing HUD event
                GameEvents.BiddingComplete(player, trump);
                _ctx.StateMachine.TransitionTo(GameStateId.PlayTrick);
            }
            else
            {
                // Pass — advance to next player
                _bidIndex++;
                if (_bidIndex >= _order.Count)
                {
                    // All four players passed → redeal
                    GameEvents.BiddingAllPassed();
                    _ctx.StateMachine.TransitionTo(GameStateId.DealCards);
                }
                else
                {
                    RequestCurrentBidderTurn();
                }
            }
        }

        public void Exit(GameContext ctx) { }
    }
}
