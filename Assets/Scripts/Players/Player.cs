using System;
using System.Collections.Generic;
using Blot.Cards;
using Blot.Gameplay;
using Blot.Gameplay.Rules;

namespace Blot.Players
{
    public enum TeamId { TeamA, TeamB }

    public abstract class Player
    {
        public int    Id   { get; }
        public string Name { get; }
        public TeamId Team { get; }

        public IReadOnlyList<Card> Hand => _hand;
        private readonly List<Card> _hand = new();

        // ---- events --------------------------------------------------------
        /// <summary>Fires after this player commits to a card (card already removed from hand).</summary>
        public event Action<Card> OnCardChosen;

        /// <summary>Fires when the player submits a bid. null = Pass.</summary>
        public event Action<Suit?> OnBidChosen;

        protected Player(int id, string name, TeamId team)
        {
            Id   = id;
            Name = name;
            Team = team;
        }

        // ---- hand management -----------------------------------------------
        public void AddCards(List<Card> cards) => _hand.AddRange(cards);
        public void ClearHand()               => _hand.Clear();

        // ---- trick play ----------------------------------------------------
        /// <summary>
        /// Called by PlayTrickState to request a card.
        /// AI responds synchronously; HumanPlayer defers until a UI click.
        /// <paramref name="allPlayers"/> is used by rule validation.
        /// </summary>
        public abstract void RequestPlay(Trick currentTrick, Suit trump, IReadOnlyList<Player> allPlayers);

        /// <summary>Returns the legally playable cards for this player.</summary>
        public List<Card> GetValidCards(Trick trick, Suit trump, IReadOnlyList<Player> allPlayers) =>
            TrickRules.GetValidCards(this, trick, trump, allPlayers);

        /// <summary>Removes <paramref name="card"/> from hand and broadcasts the choice.</summary>
        protected void CommitCard(Card card)
        {
            _hand.Remove(card);
            OnCardChosen?.Invoke(card);
        }

        // ---- bidding -------------------------------------------------------
        /// <summary>
        /// Called by BiddingState when it is this player's turn to bid.
        /// Implementations fire <see cref="OnBidChosen"/> (null = Pass, Suit = bid).
        /// </summary>
        public abstract void RequestBid();

        /// <summary>Broadcasts the bid and clears waiting state.</summary>
        protected void CommitBid(Suit? bid) => OnBidChosen?.Invoke(bid);
    }
}
