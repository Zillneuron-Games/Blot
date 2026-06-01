using System.Collections.Generic;
using Blot.Bidding;
using Blot.Cards;
using Blot.Core.StateMachine;
using Blot.Gameplay.Events;
using Blot.Players;
using UnityEngine;

namespace Blot.Gameplay.States
{
    /// <summary>
    /// Drives the full bidding / negotiation phase.
    ///
    /// Turn order:
    ///   Starts with the current Round Starter and proceeds clockwise.
    ///   Players may Pass or make a bid (value ≥ 8, higher than any current bid).
    ///   Bidding ends when 3 consecutive players pass after the last bid.
    ///   If all 4 players pass without any bid, the round is redealt
    ///   (Round Starter does NOT advance on redeal).
    ///   The Round Starter always leads trick 1 regardless of who wins the bid.
    ///
    /// Bid values represent target scores × 10 (e.g. bid 10 = target 100 pts).
    /// NoTrump is a valid trump choice — no suit acts as trump that round.
    /// </summary>
    public class BiddingState : IGameState
    {
        public GameStateId StateId => GameStateId.Bidding;

        private GameContext  _ctx;
        private List<Player> _order;          // 4 players starting from Round Starter, clockwise
        private int          _turnIndex;      // wraps: _order[_turnIndex % 4]

        private Bid          _currentBid;     // highest bid placed so far (null = no bid yet)
        private Player       _currentBidder;  // player who made _currentBid
        private int          _consecutivePasses; // passes since the last bid (or since start)

        public void Enter(GameContext ctx)
        {
            _ctx               = ctx;
            _turnIndex         = 0;
            _currentBid        = null;
            _currentBidder     = null;
            _consecutivePasses = 0;

            int startIdx = ctx.RoundManager.RoundStarterIndex;
            _order = new List<Player>(4);
            for (int i = 0; i < 4; i++)
                _order.Add(ctx.MatchManager.Players[(startIdx + i) % 4]);

            Debug.Log($"[Bidding Start] Round Starter = Player{ctx.RoundManager.CurrentRoundStarter.Id} " +
                      $"({ctx.RoundManager.CurrentRoundStarter.Name})");

            RequestCurrentBidderTurn();
        }

        // ------------------------------------------------------------------

        private Player CurrentPlayer => _order[_turnIndex % 4];

        private void RequestCurrentBidderTurn()
        {
            var player     = CurrentPlayer;
            int minimumBid = _currentBid == null ? 8 : _currentBid.Value + 1;

            player.OnBidChosen += HandleBidChosen;
            GameEvents.BidRequested(player);
            Debug.Log($"[Bidding Turn] Current Player = Player{player.Id} ({player.Name}) | " +
                      $"MinBid = {minimumBid}");
            player.RequestBid(minimumBid);
        }

        private void HandleBidChosen(Bid bid)
        {
            var player = CurrentPlayer;
            player.OnBidChosen -= HandleBidChosen;

            if (bid != null && !IsValidBid(bid))
            {
                Debug.LogWarning($"[Bidding] Player{player.Id} submitted invalid bid {bid} " +
                                 $"(minimum {(_currentBid == null ? 8 : _currentBid.Value + 1)}). " +
                                 $"Treating as Pass.");
                bid = null;
            }

            if (bid != null)
            {
                _currentBid        = bid;
                _currentBidder     = player;
                _consecutivePasses = 0;

                Debug.Log($"[Bid] Player{player.Id} bids {bid}");
                GameEvents.BidPlaced(player, bid);
            }
            else
            {
                _consecutivePasses++;
                Debug.Log($"[Pass] Player{player.Id} passes. " +
                          $"Consecutive passes: {_consecutivePasses}");
                GameEvents.BidPlaced(player, null);
            }

            // All 4 players passed without any bid → redeal.
            if (_currentBid == null && _consecutivePasses >= 4)
            {
                Debug.Log("[Bidding] All players passed. Redealing.");
                GameEvents.BiddingAllPassed();
                _ctx.StateMachine.TransitionTo(GameStateId.DealCards);
                return;
            }

            // 3 consecutive passes after the last bid → bidding ends.
            if (_currentBid != null && _consecutivePasses >= 3)
            {
                FinishBidding();
                return;
            }

            _turnIndex++;
            RequestCurrentBidderTurn();
        }

        private bool IsValidBid(Bid bid)
        {
            if (bid.Value < 8) return false;
            return _currentBid == null ? bid.Value >= 8 : bid.Value > _currentBid.Value;
        }

        private void FinishBidding()
        {
            Debug.Log($"[Bidding End] Contract = {_currentBidder.Team}, " +
                      $"Player{_currentBidder.Id} ({_currentBidder.Name}), " +
                      $"{_currentBid}, Target = {_currentBid.Value * 10}");

            _ctx.RoundManager.StartRound(_currentBid.Suit, _currentBidder, _currentBid.Value);

            // Keep HUD working — TrumpSelected still fires even for NoTrump.
            GameEvents.TrumpSelected(_currentBid.Suit);
            GameEvents.BiddingComplete(_currentBidder, _currentBid);
            _ctx.StateMachine.TransitionTo(GameStateId.PlayTrick);
        }

        public void Exit(GameContext ctx) { }
    }
}
