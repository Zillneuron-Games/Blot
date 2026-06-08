using System.Collections.Generic;
using Blot.Bidding;
using Blot.Cards;
using Blot.Players;

namespace Blot.AI
{
    /// <summary>
    /// Tracks all public game information an AI player may legitimately observe.
    /// Updated via GameEvents subscriptions in AIPlayer — never stores hidden data.
    ///
    /// Public information tracked:
    ///   - Cards played by any player (accumulated across tricks)
    ///   - Bid history (all bids and passes)
    /// </summary>
    public class AIKnowledgeBase
    {
        private readonly HashSet<(Suit, Rank)> _playedCards = new();
        private readonly List<BidRecord>        _bidHistory  = new();

        public IReadOnlyList<BidRecord> BidHistory => _bidHistory;

        // ---- round lifecycle -----------------------------------------------

        /// <summary>Call when cards are dealt (new round starting).</summary>
        public void Reset()
        {
            _playedCards.Clear();
            _bidHistory.Clear();
        }

        // ---- event handlers ------------------------------------------------

        public void RecordCardPlayed(Card card)         => _playedCards.Add((card.Suit, card.Rank));
        public void RecordBid(Player player, Bid bid)   => _bidHistory.Add(new BidRecord(player, bid));

        // ---- card queries --------------------------------------------------

        public bool IsPlayed(Suit suit, Rank rank) => _playedCards.Contains((suit, rank));
        public bool IsPlayed(Card card)             => _playedCards.Contains((card.Suit, card.Rank));

        // ---- bid queries ---------------------------------------------------

        /// <summary>
        /// Returns the most recent non-pass, non-challenge bid (current winning bid),
        /// or null if no meaningful bid has been placed yet.
        /// </summary>
        public BidRecord GetCurrentWinningBid()
        {
            for (int i = _bidHistory.Count - 1; i >= 0; i--)
            {
                var r = _bidHistory[i];
                if (r.Bid != null && !r.Bid.IsChallenge)
                    return r;
            }
            return null;
        }

        /// <summary>
        /// Returns how many meaningful bids (non-pass, non-challenge) the given team
        /// has made this round. Used to determine statement index (0=first, 1=second…).
        /// </summary>
        public int GetTeamStatementCount(TeamId team)
        {
            int count = 0;
            foreach (var r in _bidHistory)
                if (r.Player.Team == team && r.Bid != null && !r.Bid.IsChallenge)
                    count++;
            return count;
        }
    }

    /// <summary>One entry in the bidding history. Bid == null means Pass.</summary>
    public sealed class BidRecord
    {
        public Player Player { get; }
        public Bid    Bid    { get; }   // null = Pass
        public BidRecord(Player player, Bid bid) { Player = player; Bid = bid; }
    }
}
