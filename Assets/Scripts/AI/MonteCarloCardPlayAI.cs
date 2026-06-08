using System;
using System.Collections.Generic;
using Blot.Cards;
using Blot.Gameplay;
using Blot.Players;
using UnityEngine;

namespace Blot.AI
{
    /// <summary>
    /// Monte Carlo card-play AI.
    ///
    /// For each legal card the AI could play, runs <see cref="SimulationsPerCard"/>
    /// simulations.  In each simulation:
    ///   1. Unknown cards (not in AI's hand, not yet played) are randomly
    ///      distributed to the other three players.
    ///   2. The rest of the round is played out with a simple greedy policy.
    ///   3. The AI's team raw-point score is recorded.
    ///
    /// The card with the highest average simulated score is chosen.
    ///
    /// Information used:
    ///   - Own hand (known exactly)
    ///   - Cards already played this round (from AIKnowledgeBase)
    ///   - Cards visible in the current trick (from Trick.Plays)
    ///   - Trump suit
    ///   - Player seat positions and teams
    ///
    /// Information NOT used:
    ///   - Hidden cards from other players' hands
    /// </summary>
    public class MonteCarloCardPlayAI
    {
        /// <summary>Number of random deals to simulate per candidate card.</summary>
        public int SimulationsPerCard { get; set; } = 50;

        private static readonly System.Random _rng = new();

        // All real suits (excludes NoTrump — no card has that suit)
        private static readonly Suit[] AllSuits = { Suit.Clubs, Suit.Diamonds, Suit.Hearts, Suit.Spades };
        private static readonly Rank[] AllRanks = { Rank.Seven, Rank.Eight, Rank.Nine, Rank.Ten,
                                                     Rank.Jack, Rank.Queen, Rank.King, Rank.Ace };

        // ==================================================================
        // Public API
        // ==================================================================

        /// <summary>
        /// Chooses the best card from <paramref name="validCards"/> using MC simulation.
        /// </summary>
        public Card ChooseCard(
            List<Card>            validCards,
            IReadOnlyList<Card>   myHand,
            Suit                  trump,
            Trick                 currentTrick,
            IReadOnlyList<Player> allPlayers,
            AIKnowledgeBase       knowledge,
            int                   mySeat,
            TeamId                myTeam)
        {
            if (validCards.Count == 1)
            {
                Debug.Log($"[AI Play MC] Only one legal card: {validCards[0]}");
                return validCards[0];
            }

            // --- Build unknown-card pool (cards not in my hand, not yet played) ---
            var unknown = BuildUnknownPool(myHand, currentTrick, knowledge);

            // --- Pre-compute trick state ---
            // completedTricks = how many full tricks have been completed before this one
            int completedTricks = 8 - myHand.Count;  // I haven't played this trick yet

            bool[] hasPlayedTrick = new bool[4];
            var    initialTrick   = new List<(int seat, SimCard card)>(4);

            foreach (var (player, card) in currentTrick.Plays)
            {
                int s = FindSeat(allPlayers, player);
                if (s < 0) continue;
                hasPlayedTrick[s] = true;
                initialTrick.Add((s, new SimCard(card)));
            }

            int leaderSeat = currentTrick.CardCount > 0
                ? FindSeat(allPlayers, currentTrick.Plays[0].Player)
                : mySeat;

            TeamId[] teams = new TeamId[4];
            for (int i = 0; i < 4; i++) teams[i] = allPlayers[i].Team;

            // --- Evaluate each candidate card ---
            float[] ev = new float[validCards.Count];

            for (int ci = 0; ci < validCards.Count; ci++)
            {
                float total = 0f;

                for (int sim = 0; sim < SimulationsPerCard; sim++)
                {
                    ShuffleInPlace(unknown);
                    total += RunSimulation(
                        validCards[ci], myHand, unknown, trump,
                        mySeat, myTeam, teams,
                        initialTrick, hasPlayedTrick, leaderSeat, completedTricks);
                }

                ev[ci] = total / SimulationsPerCard;
                Debug.Log($"[AI Play MC] {validCards[ci]}: EV={ev[ci]:F1}");
            }

            // --- Choose highest EV ---
            int best = 0;
            for (int i = 1; i < ev.Length; i++)
                if (ev[i] > ev[best]) best = i;

            Debug.Log($"[AI Play MC] Selected: {validCards[best]} (EV={ev[best]:F1})");
            return validCards[best];
        }

        // ==================================================================
        // Simulation
        // ==================================================================

        private float RunSimulation(
            Card                         myCard,
            IReadOnlyList<Card>          myHand,
            List<SimCard>                unknownPool,       // pre-shuffled
            Suit                         trump,
            int                          mySeat,
            TeamId                       myTeam,
            TeamId[]                     teams,
            List<(int seat, SimCard card)> initialTrick,   // plays already in current trick
            bool[]                       hasPlayedTrick,
            int                          leaderSeat,
            int                          completedTricks)
        {
            // ---- Set up simulated hands ----
            var simHands = new List<SimCard>[4];
            for (int i = 0; i < 4; i++) simHands[i] = new List<SimCard>(8);

            // My hand after removing the card I'm playing
            foreach (var c in myHand)
                if (!(c.Suit == myCard.Suit && c.Rank == myCard.Rank))
                    simHands[mySeat].Add(new SimCard(c));

            // Distribute unknown cards to other players
            // Each player needs:
            //   futureCards = 8 - completedTricks - 1  (for tricks after the current one)
            //   +1 if they haven't played the current trick yet
            int poolIdx = 0;
            for (int i = 0; i < 4; i++)
            {
                if (i == mySeat) continue;
                int futureCards = Math.Max(0, 8 - completedTricks - 1);
                int currentCard = hasPlayedTrick[i] ? 0 : 1;
                int need        = Math.Min(futureCards + currentCard, unknownPool.Count - poolIdx);
                for (int j = 0; j < need; j++, poolIdx++)
                    simHands[i].Add(unknownPool[poolIdx]);
            }

            // ---- Complete the current trick ----
            var trickPlays = new List<(int seat, SimCard card)>(4);
            foreach (var p in initialTrick) trickPlays.Add(p);

            // Add my card
            trickPlays.Add((mySeat, new SimCard(myCard)));

            // Simulate the remaining seats in this trick (clockwise after leader)
            for (int offset = 1; offset <= 3; offset++)
            {
                int seat = (leaderSeat + offset) % 4;
                if (hasPlayedTrick[seat] || seat == mySeat) continue;

                var legal  = GetLegalCards(simHands[seat], trickPlays, seat, teams, trump);
                if (legal.Count == 0) legal = simHands[seat]; // safety
                var chosen = SimChoose(legal, trickPlays, seat, teams, trump);
                RemoveCard(simHands[seat], chosen);
                trickPlays.Add((seat, chosen));
            }

            // ---- Score current trick ----
            int[] teamPts  = new int[2];
            int   winSeat  = TrickWinner(trickPlays, trump);
            int   trickPts = 0;
            foreach (var (_, c) in trickPlays) trickPts += c.GetPoints(trump);
            teamPts[(int)teams[winSeat]] += trickPts;

            // ---- Play remaining tricks ----
            int nextLeader = winSeat;
            int tricksLeft = simHands[mySeat].Count;  // cards left in my simulated hand

            for (int t = 0; t < tricksLeft; t++)
            {
                var trick = new List<(int seat, SimCard card)>(4);

                for (int offset = 0; offset < 4; offset++)
                {
                    int  seat  = (nextLeader + offset) % 4;
                    var  legal = GetLegalCards(simHands[seat], trick, seat, teams, trump);
                    if (legal.Count == 0) legal = simHands[seat]; // safety
                    var  card  = SimChoose(legal, trick, seat, teams, trump);
                    RemoveCard(simHands[seat], card);
                    trick.Add((seat, card));
                }

                int w    = TrickWinner(trick, trump);
                int pts2 = 0;
                foreach (var (_, c) in trick) pts2 += c.GetPoints(trump);
                if (t == tricksLeft - 1) pts2 += 10;   // last-trick bonus
                teamPts[(int)teams[w]] += pts2;
                nextLeader = w;
            }

            return teamPts[(int)myTeam];
        }

        // ==================================================================
        // Simulation play policy
        // ==================================================================

        /// <summary>
        /// Simple greedy policy used during simulation.
        /// If partner is winning: play lowest legal card (don't waste).
        /// If you can beat the current best: play the highest winning card.
        /// Otherwise: play the lowest legal card.
        /// </summary>
        private static SimCard SimChoose(
            List<SimCard>                  legal,
            List<(int seat, SimCard card)> trick,
            int                            mySeat,
            TeamId[]                       teams,
            Suit                           trump)
        {
            if (legal.Count == 1) return legal[0];

            Suit leadSuit = trick.Count > 0 ? trick[0].card.Suit : trump;

            // Leading: play lowest to preserve strong cards
            if (trick.Count == 0) return GetLowest(legal, trump, leadSuit);

            int  bestSeat     = TrickWinner(trick, trump);
            bool partnerWins  = teams[bestSeat] == teams[mySeat] && bestSeat != mySeat;

            if (partnerWins) return GetLowest(legal, trump, leadSuit);

            var winning = GetWinningCards(legal, trick, trump, leadSuit);
            if (winning.Count > 0) return GetHighest(winning, trump, leadSuit);

            return GetLowest(legal, trump, leadSuit);
        }

        // ==================================================================
        // Legal card filtering (mirrors TrickRules)
        // ==================================================================

        private static List<SimCard> GetLegalCards(
            List<SimCard>                  hand,
            List<(int seat, SimCard card)> trick,
            int                            mySeat,
            TeamId[]                       teams,
            Suit                           trump)
        {
            if (hand.Count == 0) return new List<SimCard>();

            // Leading: any card
            if (trick.Count == 0) return new List<SimCard>(hand);

            Suit leadSuit = trick[0].card.Suit;

            // Must follow lead suit
            var suitCards = FilterBySuit(hand, leadSuit);
            if (suitCards.Count > 0) return suitCards;

            // Partner-winning exception: play anything
            int  bestSeat     = TrickWinner(trick, trump);
            bool partnerWins  = teams[bestSeat] == teams[mySeat] && bestSeat != mySeat;
            if (partnerWins) return new List<SimCard>(hand);

            // Must trump if possible
            var trumpCards = FilterBySuit(hand, trump);
            if (trumpCards.Count == 0) return new List<SimCard>(hand);

            // Must overtrump if possible
            SimCard? highestTrumpInTrick = null;
            foreach (var (_, c) in trick)
            {
                if (c.Suit == trump)
                {
                    if (!highestTrumpInTrick.HasValue ||
                        c.TrumpStrength > highestTrumpInTrick.Value.TrumpStrength)
                        highestTrumpInTrick = c;
                }
            }

            if (!highestTrumpInTrick.HasValue) return trumpCards;

            var overtrump = new List<SimCard>();
            foreach (var c in trumpCards)
                if (c.TrumpStrength > highestTrumpInTrick.Value.TrumpStrength)
                    overtrump.Add(c);

            return overtrump.Count > 0 ? overtrump : trumpCards;
        }

        // ==================================================================
        // Trick winner
        // ==================================================================

        private static int TrickWinner(List<(int seat, SimCard card)> plays, Suit trump)
        {
            if (plays.Count == 0) return -1;

            bool hasTrump = false;
            foreach (var (_, c) in plays) if (c.Suit == trump) { hasTrump = true; break; }

            Suit leadSuit = plays[0].card.Suit;
            var  best     = plays[0];

            foreach (var play in plays)
            {
                if (hasTrump)
                {
                    if (play.card.Suit == trump)
                    {
                        if (best.card.Suit != trump ||
                            play.card.TrumpStrength > best.card.TrumpStrength)
                            best = play;
                    }
                }
                else
                {
                    if (play.card.Suit == leadSuit &&
                        play.card.NonTrumpStrength > best.card.NonTrumpStrength)
                        best = play;
                }
            }

            return best.seat;
        }

        // ==================================================================
        // Card helpers
        // ==================================================================

        private static List<SimCard> FilterBySuit(List<SimCard> hand, Suit suit)
        {
            var result = new List<SimCard>();
            foreach (var c in hand) if (c.Suit == suit) result.Add(c);
            return result;
        }

        private static List<SimCard> GetWinningCards(
            List<SimCard>                  candidates,
            List<(int seat, SimCard card)> trick,
            Suit                           trump,
            Suit                           leadSuit)
        {
            int     bestSeat = TrickWinner(trick, trump);
            SimCard bestCard = default;
            foreach (var (seat, card) in trick)
                if (seat == bestSeat) { bestCard = card; break; }

            bool bestIsTrump = bestCard.Suit == trump;

            var result = new List<SimCard>();
            foreach (var c in candidates)
            {
                bool wins;
                if (c.Suit == trump)
                    wins = !bestIsTrump || c.TrumpStrength > bestCard.TrumpStrength;
                else
                    wins = !bestIsTrump && c.Suit == leadSuit &&
                           c.NonTrumpStrength > bestCard.NonTrumpStrength;

                if (wins) result.Add(c);
            }
            return result;
        }

        private static SimCard GetLowest(List<SimCard> cards, Suit trump, Suit leadSuit)
        {
            SimCard low    = cards[0];
            int     lowKey = SortKey(low, trump, leadSuit);
            foreach (var c in cards)
            {
                int k = SortKey(c, trump, leadSuit);
                if (k < lowKey) { lowKey = k; low = c; }
            }
            return low;
        }

        private static SimCard GetHighest(List<SimCard> cards, Suit trump, Suit leadSuit)
        {
            SimCard high    = cards[0];
            int     highKey = SortKey(high, trump, leadSuit);
            foreach (var c in cards)
            {
                int k = SortKey(c, trump, leadSuit);
                if (k > highKey) { highKey = k; high = c; }
            }
            return high;
        }

        // Sort key: trumps > lead-suit > other; within category by rank strength.
        private static int SortKey(SimCard c, Suit trump, Suit leadSuit)
        {
            if (c.Suit == trump)    return 100 + c.TrumpStrength;
            if (c.Suit == leadSuit) return  50 + c.NonTrumpStrength;
            return c.NonTrumpStrength;
        }

        private static void RemoveCard(List<SimCard> hand, SimCard card)
        {
            for (int i = 0; i < hand.Count; i++)
            {
                if (hand[i].Suit == card.Suit && hand[i].Rank == card.Rank)
                {
                    hand.RemoveAt(i);
                    return;
                }
            }
        }

        // ==================================================================
        // Unknown card pool
        // ==================================================================

        /// <summary>
        /// Returns SimCards for every card that is not in myHand and not yet played.
        /// Cards already visible in currentTrick.Plays are excluded via the
        /// knowledge base (CardPlayed fires before the next RequestPlay is called).
        /// </summary>
        private static List<SimCard> BuildUnknownPool(
            IReadOnlyList<Card> myHand,
            Trick               currentTrick,
            AIKnowledgeBase     knowledge)
        {
            var pool = new List<SimCard>(32);

            foreach (Suit s in AllSuits)
            {
                foreach (Rank r in AllRanks)
                {
                    // Skip if already played (includes current trick plays via OnCardPlayed)
                    if (knowledge.IsPlayed(s, r)) continue;

                    // Skip if in my hand
                    bool inHand = false;
                    foreach (var c in myHand)
                        if (c.Suit == s && c.Rank == r) { inHand = true; break; }
                    if (inHand) continue;

                    pool.Add(new SimCard(s, r));
                }
            }

            return pool;
        }

        // ==================================================================
        // Utility
        // ==================================================================

        private static int FindSeat(IReadOnlyList<Player> players, Player player)
        {
            for (int i = 0; i < players.Count; i++)
                if (players[i] == player) return i;
            return -1;
        }

        private static void ShuffleInPlace(List<SimCard> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        // ==================================================================
        // SimCard value-type — avoids heap allocation per card in simulation
        // ==================================================================

        private readonly struct SimCard
        {
            public readonly Suit Suit;
            public readonly Rank Rank;

            public SimCard(Suit suit, Rank rank) { Suit = suit; Rank = rank; }
            public SimCard(Card c)               { Suit = c.Suit; Rank = c.Rank; }

            public int TrumpStrength => Rank switch
            {
                Rank.Jack  => 7,
                Rank.Nine  => 6,
                Rank.Ace   => 5,
                Rank.Ten   => 4,
                Rank.King  => 3,
                Rank.Queen => 2,
                Rank.Eight => 1,
                _          => 0
            };

            public int NonTrumpStrength => Rank switch
            {
                Rank.Ace   => 7,
                Rank.Ten   => 6,
                Rank.King  => 5,
                Rank.Queen => 4,
                Rank.Jack  => 3,
                Rank.Nine  => 2,
                Rank.Eight => 1,
                _          => 0
            };

            public int GetPoints(Suit trump)
            {
                if (trump == Suit.NoTrump)
                    return Rank switch
                    {
                        Rank.Ace   => 19,
                        Rank.Ten   => 10,
                        Rank.King  => 4,
                        Rank.Queen => 3,
                        Rank.Jack  => 2,
                        _          => 0
                    };

                if (Suit == trump)
                    return Rank switch
                    {
                        Rank.Jack  => 20,
                        Rank.Nine  => 14,
                        Rank.Ace   => 11,
                        Rank.Ten   => 10,
                        Rank.King  => 4,
                        Rank.Queen => 3,
                        _          => 0
                    };

                return Rank switch
                {
                    Rank.Ace   => 11,
                    Rank.Ten   => 10,
                    Rank.King  => 4,
                    Rank.Queen => 3,
                    Rank.Jack  => 2,
                    _          => 0
                };
            }

            public override string ToString() => $"{Rank} of {Suit}";
        }
    }
}
