using System;
using System.Collections.Generic;
using Blot.Cards;
using Blot.Players;

namespace Blot.Gameplay.Rules
{
    /// <summary>
    /// Stateless rule engine for trick-taking.
    /// Single entry point: <see cref="GetValidCards"/> returns every card a player is
    /// legally allowed to play given the current trick state.
    ///
    /// Rules enforced (in priority order):
    ///   1. Lead — any card.
    ///   2. Must follow lead suit if possible.
    ///   3. Partner-winning exception — if partner is currently winning AND player
    ///      cannot follow lead suit, the player may play any card.
    ///   4. Must trump if player cannot follow suit (and partner is not winning).
    ///   5. Must overtrump — if trump has already been played the player must beat
    ///      the current highest trump; if they cannot, any trump is acceptable.
    /// </summary>
    public static class TrickRules
    {
        public static List<Card> GetValidCards(
            Player              player,
            Trick               trick,
            Suit                trump,
            IReadOnlyList<Player> allPlayers)
        {
            var hand = player.Hand;

            // ---- Rule 1: leading — any card --------------------------------
            if (trick.CardCount == 0)
                return new List<Card>(hand);

            Suit leadSuit = trick.LeadSuit;

            // ---- Rule 2: must follow lead suit -----------------------------
            var suitCards = Filter(hand, c => c.Suit == leadSuit);
            if (suitCards.Count > 0)
                return suitCards;

            // Player cannot follow suit from here onward.

            // ---- Rule 3: partner-winning exception -------------------------
            // If partner is currently winning the trick the player may play freely.
            if (IsPartnerWinning(player, trick))
                return new List<Card>(hand);

            // ---- Rule 4: must trump if possible ----------------------------
            var trumpCards = Filter(hand, c => c.Suit == trump);
            if (trumpCards.Count == 0)
                return new List<Card>(hand);  // no trump — play anything

            // ---- Rule 5: must overtrump ------------------------------------
            Card highestTrumpPlayed = GetHighestTrumpPlayed(trick, trump);

            if (highestTrumpPlayed == null)
                return trumpCards;  // no trump in trick yet — any trump is fine

            // Trump is already in the trick — must play a higher trump if possible.
            var overtrumpCards = Filter(trumpCards,
                c => c.GetTrumpStrength() > highestTrumpPlayed.GetTrumpStrength());

            return overtrumpCards.Count > 0 ? overtrumpCards : trumpCards;
        }

        // ------------------------------------------------------------------ helpers

        /// <summary>
        /// Returns true when the player's team-mate (same team, different player)
        /// is currently winning the trick.
        /// </summary>
        private static bool IsPartnerWinning(Player player, Trick trick)
        {
            if (trick.CardCount == 0) return false;

            var currentWinner = trick.GetCurrentWinner();
            return currentWinner != null
                && currentWinner.Team == player.Team
                && currentWinner.Id   != player.Id;
        }

        private static Card GetHighestTrumpPlayed(Trick trick, Suit trump)
        {
            Card highest = null;
            foreach (var play in trick.Plays)
            {
                if (play.Card.Suit != trump) continue;
                if (highest == null || play.Card.GetTrumpStrength() > highest.GetTrumpStrength())
                    highest = play.Card;
            }
            return highest;
        }

        private static List<Card> Filter(IReadOnlyList<Card> source, Func<Card, bool> predicate)
        {
            var result = new List<Card>();
            foreach (var card in source)
                if (predicate(card)) result.Add(card);
            return result;
        }
    }
}
