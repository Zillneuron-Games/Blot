using System.Collections.Generic;
using Blot.Bidding;
using Blot.Cards;
using Blot.Players;
using UnityEngine;

namespace Blot.AI
{
    /// <summary>
    /// Decides the AI's bid each turn following Belote bidding conventions.
    ///
    /// Statement model (first / second / third):
    ///   A "statement" is any non-pass, non-challenge bid.
    ///   GetTeamStatementCount(myTeam) on the knowledge base gives how many
    ///   statements the team has already made this round.
    ///
    /// Decision flow:
    ///   1. Challenge evaluation (only when CanChallengeNow)
    ///   2. Own-team bid  → support teammate's suit/no-trump
    ///   3. No bid / opponent bid → self-bid with best suit or no-trump
    ///
    /// All decisions are derived from the AI's own hand and the public bid history.
    /// </summary>
    public static class AIBiddingAdvisor
    {
        private const float MinSelfBidConfidence   = 0.30f;
        private const float ChallengeFailThreshold = 0.62f;  // challenge if estimated opponent fail ≥ this
        private const float SureResponseThreshold  = 0.60f;

        // ==================================================================
        // Public entry points
        // ==================================================================

        /// <summary>
        /// Returns the bid for this turn, or null for Pass.
        /// </summary>
        public static Bid ChooseBid(
            IReadOnlyList<Card> hand,
            int                 minimumBid,
            bool                canChallenge,
            bool                onlyKaput,
            AIKnowledgeBase     knowledge,
            TeamId              myTeam,
            string              name)
        {
            // Kaput counter-bid: not worth the risk — always pass.
            if (onlyKaput)
            {
                Debug.Log($"[AI Bid] {name}: OnlyKaput active — passing");
                return null;
            }

            // --- Challenge evaluation first ---
            if (canChallenge)
            {
                var challengeBid = EvaluateChallenge(hand, knowledge, name);
                if (challengeBid != null) return challengeBid;
            }

            // --- Determine current context ---
            var  winBid      = knowledge.GetCurrentWinningBid();
            bool isTeamBid   = winBid != null && winBid.Player.Team == myTeam;
            int  teamStmts   = knowledge.GetTeamStatementCount(myTeam);

            if (isTeamBid)
            {
                // Teammate holds the bid → support their suit/no-trump
                bool teamIsNT = winBid.Bid.Suit == Suit.NoTrump;
                return EvaluateSupportBid(hand, minimumBid, winBid.Bid.Suit, teamIsNT, teamStmts, name);
            }
            else
            {
                // No bid yet, or opponent holds current bid → propose own hand
                return EvaluateSelfBid(hand, minimumBid, teamStmts, name);
            }
        }

        /// <summary>
        /// Decides whether to respond "I'm Sure" to a challenge.
        /// </summary>
        public static bool ChooseSureResponse(
            IReadOnlyList<Card> hand,
            Suit                trump,
            int                 contractBidValue,
            string              name)
        {
            float conf = trump == Suit.NoTrump
                ? AIHandEvaluator.NoTrumpContractConfidence(hand, contractBidValue)
                : AIHandEvaluator.TrumpContractConfidence(hand, trump, contractBidValue);

            Debug.Log($"[AI Bid] {name}: Sure-response confidence={conf:F2} threshold={SureResponseThreshold}");
            return conf >= SureResponseThreshold;
        }

        // ==================================================================
        // Self-bid (no bid yet, or outbidding opponent)
        // ==================================================================

        private static Bid EvaluateSelfBid(
            IReadOnlyList<Card> hand,
            int                 minimumBid,
            int                 statementIdx,
            string              name)
        {
            var (bestSuit, trumpInc) = AIHandEvaluator.BestTrumpCandidate(hand);
            int ntInc                = AIHandEvaluator.NoTrumpBidIncrement(hand);

            // Choose the stronger option
            int  chosenInc;
            Suit chosenSuit;
            bool isNoTrump;

            if (trumpInc >= ntInc && trumpInc > 0)
            {
                chosenInc  = trumpInc;
                chosenSuit = bestSuit;
                isNoTrump  = false;
            }
            else if (ntInc > 0)
            {
                chosenInc  = ntInc;
                chosenSuit = Suit.NoTrump;
                isNoTrump  = true;
            }
            else
            {
                Debug.Log($"[AI Bid] {name}: Self-bid: no viable hand (no Jack / no Aces) — passing");
                return null;
            }

            // Second or later statement: limit raise to +1
            if (statementIdx >= 1) chosenInc = 1;

            // Compute target bid value
            int target = minimumBid == 8
                ? 7 + chosenInc                 // first bid of round: 8–11
                : minimumBid - 1 + chosenInc;   // raise from current winning bid by chosenInc

            target = System.Math.Min(target, 16);

            if (target < minimumBid)
            {
                Debug.Log($"[AI Bid] {name}: Self-bid target {target} < min {minimumBid} — passing");
                return null;
            }

            float conf = isNoTrump
                ? AIHandEvaluator.NoTrumpContractConfidence(hand, target)
                : AIHandEvaluator.TrumpContractConfidence(hand, chosenSuit, target);

            Debug.Log($"[AI Bid] {name}: Self-bid {(isNoTrump ? "NT" : chosenSuit.ToString())} " +
                      $"inc={chosenInc} target={target} conf={conf:F2} stmt={statementIdx}");

            if (conf < MinSelfBidConfidence)
            {
                Debug.Log($"[AI Bid] {name}: Confidence {conf:F2} too low — passing");
                return null;
            }

            return isNoTrump ? new Bid(target, Suit.NoTrump) : new Bid(target, chosenSuit);
        }

        // ==================================================================
        // Support bid (teammate holds the bid)
        // ==================================================================

        private static Bid EvaluateSupportBid(
            IReadOnlyList<Card> hand,
            int                 minimumBid,
            Suit                teamSuit,
            bool                isNoTrump,
            int                 statementIdx,
            string              name)
        {
            int raise;

            switch (statementIdx)
            {
                case 0:
                    // First statement support
                    raise = isNoTrump
                        ? AIHandEvaluator.NoTrumpBidIncrement(hand)       // Aces
                        : AIHandEvaluator.TrumpSupportRaise(hand, teamSuit);
                    break;
                case 1:
                    // Second statement support
                    raise = isNoTrump
                        ? AIHandEvaluator.CountTens(hand)                 // Tens for NT
                        : AIHandEvaluator.CountAces(hand);                 // Aces for trump
                    break;
                default:
                    // Third statement: cautious +1
                    raise = 1;
                    break;
            }

            if (raise == 0)
            {
                Debug.Log($"[AI Bid] {name}: Support {(isNoTrump ? "NT" : teamSuit.ToString())} " +
                          $"stmt={statementIdx}: no raise cards — passing");
                return null;
            }

            int target = minimumBid - 1 + raise;
            target     = System.Math.Min(target, 16);

            if (target < minimumBid)
            {
                Debug.Log($"[AI Bid] {name}: Support target {target} < min {minimumBid} — passing");
                return null;
            }

            Debug.Log($"[AI Bid] {name}: Support {(isNoTrump ? "NT" : teamSuit.ToString())} " +
                      $"raise={raise} target={target} stmt={statementIdx}");

            return isNoTrump ? new Bid(target, Suit.NoTrump) : new Bid(target, teamSuit);
        }

        // ==================================================================
        // Challenge ("I Don't Believe")
        // ==================================================================

        private static Bid EvaluateChallenge(
            IReadOnlyList<Card> hand,
            AIKnowledgeBase     knowledge,
            string              name)
        {
            var winBid = knowledge.GetCurrentWinningBid();
            if (winBid == null) return null;

            Suit opponentSuit = winBid.Bid.Suit;
            bool isNoTrump    = opponentSuit == Suit.NoTrump;
            int  bidValue     = winBid.Bid.EffectiveBidValue;

            // Higher bid → more confident opponent
            float opponentConf = Clamp01((bidValue - 8f) / 8f);

            // Our defensive strength against the opponent's suit
            float defense = 0f;
            if (!isNoTrump)
            {
                foreach (var c in hand)
                {
                    if (c.Suit == opponentSuit)
                    {
                        defense += c.Rank switch
                        {
                            Rank.Jack  => 3.0f,
                            Rank.Nine  => 2.0f,
                            Rank.Ace   => 1.0f,
                            Rank.Ten   => 0.5f,
                            _          => 0.2f
                        };
                    }
                    else if (c.Rank == Rank.Ace)
                        defense += 0.5f;
                }
            }
            else
            {
                foreach (var c in hand)
                    if (c.Rank == Rank.Ace) defense += 2f;
            }

            // Estimated probability that opponent fails to make their contract
            float failProb = Clamp01((1f - opponentConf) + defense / 12f);

            Debug.Log($"[AI Bid] {name}: Challenge eval — opponentConf={opponentConf:F2} " +
                      $"defense={defense:F1} failProb={failProb:F2} threshold={ChallengeFailThreshold}");

            if (failProb >= ChallengeFailThreshold)
            {
                Debug.Log($"[AI Bid] {name}: Challenging!");
                return Bid.MakeChallenge();
            }
            return null;
        }

        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
    }
}
