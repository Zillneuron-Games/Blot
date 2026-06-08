using System;
using System.Collections.Generic;
using Blot.Bidding;
using Blot.Cards;
using Blot.Core.StateMachine;
using Blot.Gameplay.Events;
using Blot.Players;
using Blot.UI;
using UnityEngine;

namespace Blot.Gameplay.States
{
    /// <summary>
    /// Drives the full bidding / negotiation phase, including the optional
    /// "I Don't Believe" challenge and "I'm Sure" confirmation.
    ///
    /// Normal bidding:
    ///   Starts with the current Round Starter and proceeds clockwise.
    ///   Players may Pass, raise the bid (value ≥ 8, higher than current), or
    ///   challenge the opposing team's bid ("I Don't Believe").
    ///   Bidding ends after 3 consecutive passes following the last bid, or
    ///   immediately when a challenge is issued.
    ///   All 4 passing with no bid → redeal (Round Starter unchanged).
    ///
    /// Kaput bidding:
    ///   A player may declare Kaput instead of a normal bid.
    ///   Kaput EffectiveBidValue = 25 + KaputExtraValue.
    ///   Once a Kaput bid exists, only a higher-value Kaput can beat it.
    ///   Normal numeric bids cannot beat a Kaput bid.
    ///   <see cref="Player.OnlyKaputBidAllowed"/> is set to true when Kaput is the current bid.
    ///
    /// Challenge ("I Don't Believe"):
    ///   A player may challenge when <see cref="Player.CanChallengeNow"/> is true.
    ///   This is set when the current bid belongs to the opposing team.
    ///   On challenge:
    ///     1. Bidding immediately stops.
    ///     2. The contract owner is asked to respond ("I'm Sure" or Pass).
    ///     3. FinishBidding records challenge state in RoundManager.
    ///
    /// Bonus calculation (applied in RoundEndState):
    ///   Challenge only       → +ContractBidValue to round winner.
    ///   Challenge + I'm Sure → +ContractBidValue × 3 to round winner.
    ///
    /// Pacing:
    ///   After every action (bid, pass, challenge, sure-response) the state defers
    ///   the next step through <see cref="GamePresentationController"/> so the human
    ///   player can read what happened.  If GamePresentationController is not in the
    ///   scene all delays are zero (no behaviour change).
    /// </summary>
    public class BiddingState : IGameState
    {
        public GameStateId StateId => GameStateId.Bidding;

        private GameContext  _ctx;
        private List<Player> _order;
        private int          _turnIndex;

        private Bid    _currentBid;
        private Player _currentBidder;
        private int    _consecutivePasses;

        private Player _challengePlayer;   // set when a challenge is issued

        // ==================================================================
        public void Enter(GameContext ctx)
        {
            _ctx               = ctx;
            _turnIndex         = 0;
            _currentBid        = null;
            _currentBidder     = null;
            _consecutivePasses = 0;
            _challengePlayer   = null;

            int startIdx = ctx.RoundManager.RoundStarterIndex;
            _order = new List<Player>(4);
            for (int i = 0; i < 4; i++)
                _order.Add(ctx.MatchManager.Players[(startIdx + i) % 4]);

            Debug.Log($"[Bidding Start] Round Starter = Player{ctx.RoundManager.CurrentRoundStarter.Id} " +
                      $"({ctx.RoundManager.CurrentRoundStarter.Name})");

            RequestCurrentBidderTurn();
        }

        // ==================================================================
        // Turn loop
        // ==================================================================

        private Player CurrentPlayer => _order[_turnIndex % 4];

        private void RequestCurrentBidderTurn()
        {
            var player     = CurrentPlayer;
            int minimumBid = _currentBid == null ? 8 : _currentBid.EffectiveBidValue + 1;

            // A challenge is legal only when the opposing team holds the current bid.
            player.CanChallengeNow = _currentBid != null
                                  && _currentBidder != null
                                  && _currentBidder.Team != player.Team;

            // Signal when only Kaput bids are valid (current bid is already Kaput).
            player.OnlyKaputBidAllowed = _currentBid != null && _currentBid.IsKaput;

            player.OnBidChosen += HandleBidChosen;
            GameEvents.BidRequested(player);

            Debug.Log($"[Bidding Turn] Player{player.Id} ({player.Name}) | " +
                      $"MinBid = {minimumBid} | CanChallenge = {player.CanChallengeNow} | " +
                      $"OnlyKaput = {player.OnlyKaputBidAllowed}");

            player.RequestBid(minimumBid);
        }

        // ==================================================================
        // Normal bid / pass
        // ==================================================================

        private void HandleBidChosen(Bid bid)
        {
            var player = CurrentPlayer;
            player.OnBidChosen -= HandleBidChosen;
            player.CanChallengeNow      = false;
            player.OnlyKaputBidAllowed  = false;

            // ---- Challenge branch ----------------------------------------
            if (bid != null && bid.IsChallenge)
            {
                HandleChallenge(player);
                return;
            }

            // ---- Validate normal/Kaput bid -------------------------------
            if (bid != null && !IsValidBid(bid))
            {
                Debug.LogWarning($"[Bidding] Player{player.Id} submitted invalid bid {bid} " +
                                 $"(min effective {(_currentBid == null ? 8 : _currentBid.EffectiveBidValue + 1)}). " +
                                 $"Treating as Pass.");
                bid = null;
            }

            // ---- Record bid or pass --------------------------------------
            if (bid != null)
            {
                _currentBid        = bid;
                _currentBidder     = player;
                _consecutivePasses = 0;

                if (bid.IsKaput)
                    Debug.Log($"[Bid] Player{player.Id} ({player.Name}) bids Kaput {bid.Suit}, " +
                              $"EffectiveValue = {bid.EffectiveBidValue}");
                else
                    Debug.Log($"[Bid] Player{player.Id} ({player.Name}) bids {bid}");

                GameEvents.BidPlaced(player, bid);
            }
            else
            {
                _consecutivePasses++;
                Debug.Log($"[Pass] Player{player.Id} ({player.Name}) passes. " +
                          $"Consecutive passes: {_consecutivePasses}");
                GameEvents.BidPlaced(player, null);
            }

            // ---- Termination checks (each deferred by pacing delay) ------
            if (_currentBid == null && _consecutivePasses >= 4)
            {
                Debug.Log("[Bidding] All players passed. Redealing.");
                GameEvents.BiddingAllPassed();
                Advance(BiddingDelay, () => _ctx.StateMachine.TransitionTo(GameStateId.DealCards));
                return;
            }

            if (_currentBid != null && _consecutivePasses >= 3)
            {
                Advance(BiddingDelay, () => FinishBidding(challenger: null, isSure: false));
                return;
            }

            Advance(BiddingDelay, () =>
            {
                _turnIndex++;
                RequestCurrentBidderTurn();
            });
        }

        private bool IsValidBid(Bid bid)
        {
            if (_currentBid == null)
            {
                if (bid.IsKaput) return true;
                return bid.Value >= 8;
            }

            if (_currentBid.IsKaput)
            {
                if (!bid.IsKaput)
                {
                    Debug.Log("[Bidding] Normal bid blocked because Kaput already exists");
                    return false;
                }
                return bid.EffectiveBidValue > _currentBid.EffectiveBidValue;
            }
            else
            {
                if (bid.IsKaput)
                    return bid.EffectiveBidValue > _currentBid.EffectiveBidValue;

                return bid.Value > _currentBid.Value;
            }
        }

        // ==================================================================
        // Challenge flow
        // ==================================================================

        private void HandleChallenge(Player challenger)
        {
            _challengePlayer = challenger;

            Debug.Log($"[Challenge] Player{challenger.Id} ({challenger.Name}) " +
                      $"says 'I Don't Believe!' against {_currentBidder.Name}'s {_currentBid}");

            GameEvents.Challenged(challenger);

            // Pause so the challenge message is visible, then ask contract owner.
            Advance(BiddingDelay, () =>
            {
                _currentBidder.OnSureResponse += HandleSureResponse;
                _currentBidder.RequestSureResponse();
            });
        }

        private void HandleSureResponse(bool isSure)
        {
            _currentBidder.OnSureResponse -= HandleSureResponse;

            Debug.Log($"[Challenge Response] Player{_currentBidder.Id} ({_currentBidder.Name}): " +
                      $"{(isSure ? "I'm Sure!" : "Pass (no confirmation)")}");

            GameEvents.ChallengeResponded(_currentBidder, isSure);

            // Pause so the response is visible, then wrap up.
            Advance(BiddingDelay, () => FinishBidding(_challengePlayer, isSure));
        }

        // ==================================================================
        // Finalise
        // ==================================================================

        private void FinishBidding(Player challenger, bool isSure)
        {
            string targetInfo = _currentBid.IsKaput
                ? $"WIN ALL TRICKS (Kaput)"
                : $"Target = {_currentBid.EffectiveBidValue * 10}";

            Debug.Log($"[Bidding End] Contract = {_currentBidder.Team}, " +
                      $"Player{_currentBidder.Id} ({_currentBidder.Name}), " +
                      $"{_currentBid}, {targetInfo}" +
                      (challenger != null ? $" | Challenged by Player{challenger.Id}" : ""));

            // StartRound resets challenge state — SetChallengeState is called after.
            _ctx.RoundManager.StartRound(
                _currentBid.Suit,
                _currentBidder,
                _currentBid.EffectiveBidValue,
                _currentBid.IsKaput,
                _currentBid.KaputExtraValue);

            if (challenger != null)
                _ctx.RoundManager.SetChallengeState(challenger, isSure);

            GameEvents.TrumpSelected(_currentBid.Suit);
            GameEvents.BiddingComplete(_currentBidder, _currentBid);
            _ctx.StateMachine.TransitionTo(GameStateId.AnnounceDeclarations);
        }

        public void Exit(GameContext ctx) { }

        // ==================================================================
        // Pacing helper
        // ==================================================================

        private static float BiddingDelay =>
            GamePresentationController.Instance?.BiddingActionDelay ?? 0f;

        /// <summary>
        /// Runs <paramref name="action"/> after the configured bidding delay.
        /// Falls back to immediate execution if no presentation controller exists.
        /// </summary>
        private static void Advance(float delay, Action action)
        {
            var pacing = GamePresentationController.Instance;
            if (pacing != null && delay > 0f)
                pacing.RunAfterDelay(delay, action);
            else
                action.Invoke();
        }
    }
}
