using System;
using System.Collections.Generic;
using Blot.Cards;
using Blot.Gameplay;
using Blot.Gameplay.Rules;
using Blot.Players;

namespace Blot.Core.Managers
{
    public class RoundManager
    {
        // ---- public state --------------------------------------------------
        public Suit   Trump           { get; private set; }
        public Trick  CurrentTrick    { get; private set; }
        public int    TricksPlayed    { get; private set; }
        public int    LeadPlayerIndex { get; private set; }

        /// <summary>All four players in seat order (index 0-3).</summary>
        public IReadOnlyList<Player> Players => _players;

        /// <summary>The player who won the bidding and chose trump.</summary>
        public Player BiddingPlayer   { get; private set; }

        /// <summary>Team of the bidder; defaults to TeamA if not yet set.</summary>
        public TeamId BiddingTeam     => BiddingPlayer?.Team ?? TeamId.TeamA;

        /// <summary>Winner of the 8th trick (for +10 last-trick bonus).</summary>
        public Player LastTrickWinner { get; private set; }

        /// <summary>Tracks Belote / Rebelote for the current round.</summary>
        public BeloteTracker BeloteTracker { get; private set; }

        // ---- private -------------------------------------------------------
        private readonly Player[] _players;

        public RoundManager(Player[] players) => _players = players;

        // ------------------------------------------------------------------ round control

        /// <summary>
        /// Initialises a new round. Call after bidding is complete.
        /// <paramref name="bidder"/> is the player who chose trump (may be null for fallback paths).
        /// </summary>
        public void StartRound(Suit trump, int leadPlayerIndex, Player bidder = null)
        {
            Trump            = trump;
            TricksPlayed     = 0;
            LeadPlayerIndex  = leadPlayerIndex;
            BiddingPlayer    = bidder;
            LastTrickWinner  = null;
            BeloteTracker    = new BeloteTracker(trump, _players);
            BeginNewTrick();
        }

        /// <summary>Creates a fresh Trick object ready to receive plays.</summary>
        public void BeginNewTrick() => CurrentTrick = new Trick(Trump);

        // ------------------------------------------------------------------ trick control

        /// <summary>Returns the four players in the order they act this trick.</summary>
        public List<Player> GetTrickTurnOrder()
        {
            var order = new List<Player>(4);
            for (int i = 0; i < 4; i++)
                order.Add(_players[(LeadPlayerIndex + i) % 4]);
            return order;
        }

        /// <summary>
        /// Records the trick as complete, updates the lead player to the winner.
        /// Also stores <see cref="LastTrickWinner"/> when trick 8 ends.
        /// </summary>
        public void CompleteTrick(Player winner)
        {
            TricksPlayed++;
            LeadPlayerIndex = Array.IndexOf(_players, winner);

            if (TricksPlayed == 8)
                LastTrickWinner = winner;

            if (TricksPlayed < 8)
                BeginNewTrick();
        }
    }
}
