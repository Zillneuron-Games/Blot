using System.Collections.Generic;
using Blot.Cards;
using Blot.Players;
using UnityEngine;

namespace Blot.UI
{
    /// <summary>
    /// Manages the row of <see cref="CardView"/> prefabs for the human player's hand.
    /// Listens to <see cref="HumanPlayer.OnCardSelectionRequested"/> to highlight valid cards.
    ///
    /// Cards are displayed sorted by Suit (Clubs → Diamonds → Hearts → Spades) then
    /// by Rank ascending (Seven → … → Ace).  The sort is display-only — the underlying
    /// <see cref="Player.Hand"/> list is never reordered, so AI logic and TrickRules
    /// are unaffected.
    /// </summary>
    public class PlayerHandView : MonoBehaviour
    {
        [SerializeField] private CardView  _cardPrefab;
        [SerializeField] private Transform _cardContainer;

        private HumanPlayer _player;
        private readonly List<CardView> _views = new();

        public void Bind(HumanPlayer player)
        {
            _player = player;
            _player.OnCardSelectionRequested += RefreshValidCards;
        }

        private void OnDestroy()
        {
            if (_player != null)
                _player.OnCardSelectionRequested -= RefreshValidCards;
        }

        /// <summary>Destroys all card views and recreates them from the player's current hand,
        /// sorted by suit then rank.</summary>
        public void RebuildHand()
        {
            foreach (var view in _views)
                Destroy(view.gameObject);
            _views.Clear();

            // Build a sorted display copy — never mutates Player.Hand.
            // Enum integer values already match the desired order:
            //   Suit: Clubs=0, Diamonds=1, Hearts=2, Spades=3  (NoTrump=4 sorted last as guard)
            //   Rank: Seven=0 … Ace=7
            var sorted = new List<Card>(_player.Hand);
            sorted.Sort((a, b) =>
            {
                int suitA = a.Suit == Suit.NoTrump ? 99 : (int)a.Suit;
                int suitB = b.Suit == Suit.NoTrump ? 99 : (int)b.Suit;
                if (suitA != suitB) return suitA.CompareTo(suitB);
                return ((int)a.Rank).CompareTo((int)b.Rank);
            });

            foreach (var card in sorted)
            {
                var view = Instantiate(_cardPrefab, _cardContainer);
                view.Bind(card, _player);   // stores the original Card reference — click = correct card
                _views.Add(view);
            }
        }

        private void RefreshValidCards(List<Card> validCards)
        {
            RebuildHand();   // sync hand after any card was removed in a previous trick

            foreach (var view in _views)
            {
                bool isValid = validCards.Exists(c => c == view.Card);
                view.SetInteractable(isValid);
            }
        }
    }
}
