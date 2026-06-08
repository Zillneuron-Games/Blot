using System.Collections.Generic;
using Blot.Cards;

namespace Blot.AI
{
    /// <summary>
    /// Stateless helpers that assess hand strength for bidding.
    /// All methods work purely from the AI's own cards — no hidden information.
    /// </summary>
    public static class AIHandEvaluator
    {
        // ---- trump bid increment -------------------------------------------
        // Maps how many "power cards" the hand has for a given trump suit
        // to a bid-level increment above minimum.
        //   0 = no Jack → don't propose this suit
        //   1 = Jack only
        //   2 = Jack + Nine
        //   3 = Jack + Nine + Ace
        //   4 = Jack + Nine + Ace + Ten

        public static int TrumpBidIncrement(IReadOnlyList<Card> hand, Suit suit)
        {
            if (!HasCard(hand, suit, Rank.Jack)) return 0;
            if (!HasCard(hand, suit, Rank.Nine)) return 1;
            if (!HasCard(hand, suit, Rank.Ace))  return 2;
            if (!HasCard(hand, suit, Rank.Ten))  return 3;
            return 4;
        }

        /// <summary>
        /// Returns the real suit (not NoTrump) with the highest TrumpBidIncrement.
        /// increment == 0 means no viable trump proposal.
        /// </summary>
        public static (Suit suit, int increment) BestTrumpCandidate(IReadOnlyList<Card> hand)
        {
            Suit bestSuit = Suit.Clubs;
            int  bestInc  = 0;
            foreach (Suit s in new[] { Suit.Clubs, Suit.Diamonds, Suit.Hearts, Suit.Spades })
            {
                int inc = TrumpBidIncrement(hand, s);
                if (inc > bestInc) { bestInc = inc; bestSuit = s; }
            }
            return (bestSuit, bestInc);
        }

        // ---- no-trump bid increment ----------------------------------------
        // One Ace = +1, two = +2, three = +3, four = +4.

        public static int NoTrumpBidIncrement(IReadOnlyList<Card> hand)
        {
            int aces = 0;
            foreach (var c in hand) if (c.Rank == Rank.Ace) aces++;
            return aces;
        }

        // ---- trump support raise -------------------------------------------
        // Raise amount when supporting a teammate's existing trump bid.
        //   Jack + Nine → +3
        //   Jack only   → +2
        //   Nine only   → +1
        //   Neither     → 0

        public static int TrumpSupportRaise(IReadOnlyList<Card> hand, Suit suit)
        {
            bool j = HasCard(hand, suit, Rank.Jack);
            bool n = HasCard(hand, suit, Rank.Nine);
            if (j && n) return 3;
            if (j)      return 2;
            if (n)      return 1;
            return 0;
        }

        // ---- counting helpers for second-statement support -----------------

        public static int CountAces(IReadOnlyList<Card> hand)
        {
            int c = 0;
            foreach (var card in hand) if (card.Rank == Rank.Ace) c++;
            return c;
        }

        public static int CountTens(IReadOnlyList<Card> hand)
        {
            int c = 0;
            foreach (var card in hand) if (card.Rank == Rank.Ten) c++;
            return c;
        }

        // ---- contract confidence -------------------------------------------

        /// <summary>
        /// Estimates probability (0–1) that a trump contract at bidValue will succeed,
        /// based purely on the AI's own hand.
        /// </summary>
        public static float TrumpContractConfidence(IReadOnlyList<Card> hand, Suit trump, int bidValue)
        {
            float score = 0f;
            foreach (var c in hand)
            {
                if (c.Suit == trump)
                {
                    score += c.Rank switch
                    {
                        Rank.Jack  => 3.0f,
                        Rank.Nine  => 2.0f,
                        Rank.Ace   => 1.5f,
                        Rank.Ten   => 1.0f,
                        Rank.King  => 0.5f,
                        _          => 0.2f
                    };
                }
                else
                {
                    score += c.Rank switch
                    {
                        Rank.Ace   => 0.8f,
                        Rank.Ten   => 0.4f,
                        Rank.King  => 0.2f,
                        _          => 0f
                    };
                }
            }
            // J+9+A+10 trump + four aces ≈ 11 → scale to 1
            float conf       = score / 11f;
            float bidPenalty = (bidValue - 8) * 0.04f;
            return Clamp01(conf - bidPenalty);
        }

        /// <summary>
        /// Estimates probability (0–1) that a No Trump contract at bidValue will succeed.
        /// </summary>
        public static float NoTrumpContractConfidence(IReadOnlyList<Card> hand, int bidValue)
        {
            float score = 0f;
            foreach (var c in hand)
            {
                score += c.Rank switch
                {
                    Rank.Ace   => 3.0f,
                    Rank.Ten   => 1.0f,
                    Rank.King  => 0.3f,
                    _          => 0f
                };
            }
            // Four aces + four tens ≈ 16 → scale to 1
            float conf       = score / 16f;
            float bidPenalty = (bidValue - 8) * 0.04f;
            return Clamp01(conf - bidPenalty);
        }

        // ---- utility -------------------------------------------------------

        public static bool HasCard(IReadOnlyList<Card> hand, Suit suit, Rank rank)
        {
            foreach (var c in hand)
                if (c.Suit == suit && c.Rank == rank) return true;
            return false;
        }

        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
    }
}
