using System;
using System.Collections.Generic;
using Blot.Cards;
using Blot.Gameplay;

namespace Blot.Players
{
    public class HumanPlayer : Player
    {
        // ---- card selection ------------------------------------------------
        /// <summary>
        /// UI subscribes here to learn which cards are currently playable.
        /// Fired every time it is this player's turn to play a card.
        /// </summary>
        public event Action<List<Card>> OnCardSelectionRequested;

        private Trick                  _pendingTrick;
        private Suit                   _pendingTrump;
        private IReadOnlyList<Player>  _pendingAllPlayers;
        private bool                   _waitingForInput;

        // ---- bidding -------------------------------------------------------
        /// <summary>
        /// UI subscribes here to show the bidding panel.
        /// Call <see cref="TryPlaceBid"/> when the player makes a choice.
        /// </summary>
        public event Action OnBidRequested;

        private bool _waitingForBid;

        /// <summary>
        /// When true (default) the player auto-bids randomly if no UI handler is wired.
        /// Set to false once a proper bidding UI is connected.
        /// </summary>
        public bool AutoBidFallback { get; set; } = true;

        // ------------------------------------------------------------------ ctor
        public HumanPlayer(int id, string name, TeamId team) : base(id, name, team) { }

        // ------------------------------------------------------------------ card play

        public override void RequestPlay(Trick currentTrick, Suit trump, IReadOnlyList<Player> allPlayers)
        {
            _pendingTrick      = currentTrick;
            _pendingTrump      = trump;
            _pendingAllPlayers = allPlayers;
            _waitingForInput   = true;

            var valid = GetValidCards(currentTrick, trump, allPlayers);
            OnCardSelectionRequested?.Invoke(valid);
        }

        /// <summary>
        /// Called by the UI when the player clicks a CardView.
        /// Returns false if the card is not in the legal set or it is not this player's turn.
        /// </summary>
        public bool TrySelectCard(Card card)
        {
            if (!_waitingForInput) return false;

            var valid = GetValidCards(_pendingTrick, _pendingTrump, _pendingAllPlayers);
            if (!valid.Contains(card)) return false;

            _waitingForInput = false;
            CommitCard(card);
            return true;
        }

        // ------------------------------------------------------------------ bidding

        public override void RequestBid()
        {
            _waitingForBid = true;
            OnBidRequested?.Invoke();       // UI hook — show the bidding panel

            if (AutoBidFallback)
            {
                // Temporary fallback: random bid until bidding UI is implemented.
                // 50 % chance to bid a random suit, 50 % to pass.
                Suit? bid = UnityEngine.Random.value > 0.5f
                    ? (Suit?)null
                    : (Suit)UnityEngine.Random.Range(0, 4);
                TryPlaceBid(bid);
            }
        }

        /// <summary>
        /// Called by the UI (or debug code) when the human chooses a bid.
        /// <paramref name="bid"/> null = Pass.
        /// Returns false if it is not currently this player's bid turn.
        /// </summary>
        public bool TryPlaceBid(Suit? bid)
        {
            if (!_waitingForBid) return false;
            _waitingForBid = false;
            CommitBid(bid);
            return true;
        }
    }
}
