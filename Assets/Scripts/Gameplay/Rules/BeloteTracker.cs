using Blot.Cards;
using Blot.Players;

namespace Blot.Gameplay.Rules
{
    public enum BeloteEvent { None, Belote, Rebelote }

    /// <summary>
    /// Tracks the Belote / Rebelote declaration for one round.
    ///
    /// Belote  = a player holds both the King and Queen of trump and plays the first one.
    /// Rebelote= the same player plays the second card of the pair.
    ///
    /// Bonus: +20 points are awarded to that player's team when Rebelote is completed.
    /// The bonus is scored externally — this class only tracks state and raises events.
    /// </summary>
    public class BeloteTracker
    {
        private readonly Suit   _trump;
        private readonly bool[] _hasPair;      // [player.Id] — owns K+Q of trump
        private readonly int[]  _pairPlayed;   // [player.Id] — how many of K/Q played

        public BeloteTracker(Suit trump, Player[] players)
        {
            _trump      = trump;
            _hasPair    = new bool[players.Length];
            _pairPlayed = new int[players.Length];

            foreach (var player in players)
            {
                bool hasKing  = false;
                bool hasQueen = false;
                foreach (var card in player.Hand)
                {
                    if (card.Suit != trump) continue;
                    if (card.Rank == Rank.King)  hasKing  = true;
                    if (card.Rank == Rank.Queen) hasQueen = true;
                }
                _hasPair[player.Id] = hasKing && hasQueen;
            }
        }

        /// <summary>
        /// Must be called whenever a card is played.
        /// Returns the appropriate <see cref="BeloteEvent"/> or <see cref="BeloteEvent.None"/>.
        /// </summary>
        public BeloteEvent NotifyCardPlayed(Player player, Card card)
        {
            if (player.Id >= _hasPair.Length) return BeloteEvent.None;
            if (!_hasPair[player.Id])          return BeloteEvent.None;
            if (card.Suit != _trump)            return BeloteEvent.None;
            if (card.Rank != Rank.King && card.Rank != Rank.Queen)
                                                return BeloteEvent.None;

            _pairPlayed[player.Id]++;

            return _pairPlayed[player.Id] switch
            {
                1 => BeloteEvent.Belote,
                2 => BeloteEvent.Rebelote,
                _ => BeloteEvent.None
            };
        }

        /// <returns>20 if the player completed Rebelote this round, otherwise 0.</returns>
        public int GetBonusFor(int playerId) =>
            playerId < _hasPair.Length && _hasPair[playerId] && _pairPlayed[playerId] >= 2 ? 20 : 0;

        /// <summary>Total Belote bonus earned by a team this round.</summary>
        public int GetTeamBonus(TeamId team, Player[] players)
        {
            int total = 0;
            foreach (var p in players)
                if (p.Team == team) total += GetBonusFor(p.Id);
            return total;
        }
    }
}
