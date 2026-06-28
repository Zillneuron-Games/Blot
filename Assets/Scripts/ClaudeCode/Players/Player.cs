using System;
using System.Collections.Generic;
using Blot.Bidding;
using Blot.Cards;
using Blot.Declarations;
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
        public event Action<Bid> OnBidChosen;

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
        /// <paramref name="minimumBid"/> is 8 if no bids have been placed, or
        /// currentHighestBid + 1 otherwise.
        /// Implementations fire <see cref="OnBidChosen"/>:
        ///   null             = Pass
        ///   Bid (normal)     = raise bid
        ///   Bid.IsChallenge  = "I Don't Believe" challenge
        /// Check <see cref="CanChallengeNow"/> to know whether a challenge is legal this turn.
        /// </summary>
        public abstract void RequestBid(int minimumBid);

        /// <summary>
        /// True when this player is legally allowed to challenge the current bid.
        /// Set by BiddingState immediately before calling <see cref="RequestBid"/>.
        /// </summary>
        public bool CanChallengeNow { get; set; }

        /// <summary>
        /// True when the current bid is a Kaput bid and only a higher Kaput can beat it.
        /// Normal numeric bids are invalid when this is true.
        /// Set by BiddingState immediately before calling <see cref="RequestBid"/>.
        /// </summary>
        public bool OnlyKaputBidAllowed { get; set; }

        /// <summary>Broadcasts the bid and clears waiting state. null = Pass.</summary>
        protected void CommitBid(Bid bid) => OnBidChosen?.Invoke(bid);

        // ---- challenge response --------------------------------------------
        /// <summary>
        /// Fires when the contract player responds to a challenge.
        /// true = "I'm Sure" / false = Pass (decline to confirm).
        /// </summary>
        public event Action<bool> OnSureResponse;

        /// <summary>
        /// Called by BiddingState when an opponent has challenged this player's contract.
        /// Implementations must fire <see cref="OnSureResponse"/> (true = I'm Sure).
        /// </summary>
        public abstract void RequestSureResponse();

        protected void CommitSureResponse(bool isSure) => OnSureResponse?.Invoke(isSure);

        // ---- declarations --------------------------------------------------

        /// <summary>
        /// Fires when the player submits their declaration announcement.
        /// Empty list = no declarations announced.
        /// </summary>
        public event Action<List<Declaration>> OnDeclarationsChosen;

        /// <summary>
        /// Fires when the player confirms (true) or skips (false) their reveal window.
        /// </summary>
        public event Action<bool> OnRevealResult;

        /// <summary>
        /// Called by AnnounceDeclarationsState before trick 1.
        /// <paramref name="trump"/> is the contract trump (may be NoTrump).
        /// Implementations must fire <see cref="OnDeclarationsChosen"/>.
        /// </summary>
        public abstract void RequestDeclare(Suit trump);

        /// <summary>
        /// Called by RevealDeclarationsState at the appropriate trick.
        /// <paramref name="toReveal"/> contains the declarations this player announced.
        /// Implementations must fire <see cref="OnRevealResult"/> (true = confirmed).
        /// </summary>
        public abstract void RequestReveal(List<Declaration> toReveal);

        protected void CommitDeclarations(List<Declaration> chosen) =>
            OnDeclarationsChosen?.Invoke(chosen ?? new List<Declaration>());

        protected void CommitReveal(bool confirmed) => OnRevealResult?.Invoke(confirmed);
    }
}
