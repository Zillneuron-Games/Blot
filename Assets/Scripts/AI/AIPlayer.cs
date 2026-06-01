using System;
using System.Collections.Generic;
using Blot.Cards;
using Blot.Gameplay;
using Blot.Players;

namespace Blot.AI
{
    /// <summary>
    /// Simple AI player.
    ///
    /// Card play: plays the lowest legally-valid card (by strength).
    /// Bidding:   50 % chance to Pass, otherwise bids a random suit.
    /// </summary>
    public class AIPlayer : Player
    {
        private static readonly Random _rng = new();

        public AIPlayer(int id, string name, TeamId team) : base(id, name, team) { }

        // ------------------------------------------------------------------ card play

        public override void RequestPlay(Trick currentTrick, Suit trump, IReadOnlyList<Player> allPlayers)
        {
            var valid  = GetValidCards(currentTrick, trump, allPlayers);
            var chosen = PickLowest(valid, trump);
            CommitCard(chosen);
        }

        private Card PickLowest(List<Card> candidates, Suit trump)
        {
            Card lowest     = candidates[0];
            int  lowestRank = Strength(lowest, trump);

            for (int i = 1; i < candidates.Count; i++)
            {
                int s = Strength(candidates[i], trump);
                if (s < lowestRank)
                {
                    lowestRank = s;
                    lowest     = candidates[i];
                }
            }
            return lowest;
        }

        private static int Strength(Card card, Suit trump) =>
            card.Suit == trump ? card.GetTrumpStrength() : card.GetNonTrumpStrength();

        // ------------------------------------------------------------------ bidding

        public override void RequestBid()
        {
            // 50 % pass — 50 % bid a random suit
            Suit? bid = _rng.Next(0, 2) == 0
                ? (Suit?)null
                : (Suit)_rng.Next(0, 4);

            CommitBid(bid);
        }
    }
}
