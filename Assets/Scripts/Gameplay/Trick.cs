using System.Collections.Generic;
using Blot.Cards;
using Blot.Players;

namespace Blot.Gameplay
{
    public class Trick
    {
        public Suit Trump { get; }

        private readonly List<(Player Player, Card Card)> _plays = new();
        public IReadOnlyList<(Player Player, Card Card)> Plays => _plays;

        public int  CardCount => _plays.Count;
        public Suit LeadSuit  => _plays.Count > 0 ? _plays[0].Card.Suit : default;

        public Trick(Suit trump) => Trump = trump;

        public void AddPlay(Player player, Card card) => _plays.Add((player, card));

        /// <summary>
        /// Returns the player currently leading the trick based on cards played so far.
        /// Safe to call on an incomplete trick. Returns null if no cards have been played.
        /// </summary>
        public Player GetCurrentWinner() => _plays.Count == 0 ? null : GetWinner();

        /// <summary>
        /// Determines the trick winner.<br/>
        /// If any trump was played the highest trump wins;
        /// otherwise the highest card of the lead suit wins.
        /// </summary>
        public Player GetWinner()
        {
            if (_plays.Count == 0) return null;

            bool hasTrump = _plays.Exists(p => p.Card.Suit == Trump);
            var  best     = _plays[0];

            foreach (var play in _plays)
            {
                if (hasTrump)
                {
                    if (play.Card.Suit == Trump)
                    {
                        // Replace best if best is not trump, or if this trump is stronger
                        if (best.Card.Suit != Trump ||
                            play.Card.GetTrumpStrength() > best.Card.GetTrumpStrength())
                        {
                            best = play;
                        }
                    }
                }
                else
                {
                    if (play.Card.Suit == LeadSuit &&
                        play.Card.GetNonTrumpStrength() > best.Card.GetNonTrumpStrength())
                    {
                        best = play;
                    }
                }
            }

            return best.Player;
        }

        /// <summary>Sum of point values for all cards played in this trick.</summary>
        public int GetTotalPoints()
        {
            int total = 0;
            foreach (var play in _plays)
                total += play.Card.GetPoints(Trump);
            return total;
        }
    }
}
