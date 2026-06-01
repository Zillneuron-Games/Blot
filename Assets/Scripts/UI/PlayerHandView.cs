using System.Collections.Generic;
using Blot.Cards;
using Blot.Players;
using UnityEngine;

namespace Blot.UI
{
    /// <summary>
    /// Manages the row of <see cref="CardView"/> prefabs for the human player's hand.
    /// Listens to <see cref="HumanPlayer.OnCardSelectionRequested"/> to highlight valid cards.
    /// </summary>
    public class PlayerHandView : MonoBehaviour
    {
        [SerializeField] private CardView  _cardPrefab;
        [SerializeField] private Transform _cardContainer;

        private HumanPlayer           _player;
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

        /// <summary>Destroys all card views and recreates them from the player's current hand.</summary>
        public void RebuildHand()
        {
            foreach (var view in _views)
                Destroy(view.gameObject);
            _views.Clear();

            foreach (var card in _player.Hand)
            {
                var view = Instantiate(_cardPrefab, _cardContainer);
                view.Bind(card, _player);
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
