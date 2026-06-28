using System.Collections.Generic;
using Blot.Cards;
using Blot.Gameplay.Events;
using Blot.Players;
using UnityEngine;
using UnityEngine.UI;

namespace Blot.UI
{
    /// <summary>
    /// Shows a row / column of card-back images for one AI player.
    /// Set <see cref="_playerId"/> in the Inspector (1 = East/right, 2 = North/top, 3 = West/left).
    /// Auto-binds to the matching player from <see cref="Blot.GameManager"/> in Start().
    /// </summary>
    public class AIHandView : MonoBehaviour
    {
        [SerializeField] private Image     _cardBackPrefab;
        [SerializeField] private Transform _container;
        [SerializeField] private int       _playerId = -1;

        private Player           _player;
        private readonly List<Image> _backs = new();

        // ------------------------------------------------------------------ lifecycle

        private void OnEnable()
        {
            GameEvents.OnCardsDealt += Rebuild;
            GameEvents.OnCardPlayed += HandleCardPlayed;
        }

        private void OnDisable()
        {
            GameEvents.OnCardsDealt -= Rebuild;
            GameEvents.OnCardPlayed -= HandleCardPlayed;
        }

        private void Start()
        {
            // Auto-bind via player index. Safe because GameManager runs at a higher
            // execution order and builds MatchManager in Awake().
            if (_player == null && _playerId >= 0 && GameManager.Instance?.MatchManager != null)
                _player = GameManager.Instance.MatchManager.Players[_playerId];

            Rebuild(); // catch-up in case OnCardsDealt already fired before Start
        }

        // ------------------------------------------------------------------ helpers

        private void Rebuild()
        {
            foreach (var b in _backs) Destroy(b.gameObject);
            _backs.Clear();

            if (_player == null || _cardBackPrefab == null) return;

            for (int i = 0; i < _player.Hand.Count; i++)
            {
                var img = Instantiate(_cardBackPrefab, _container);
                _backs.Add(img);
            }
        }

        private void HandleCardPlayed(Player player, Card _)
        {
            if (_player == null || player.Id != _player.Id) return;
            if (_backs.Count == 0) return;

            int last = _backs.Count - 1;
            Destroy(_backs[last].gameObject);
            _backs.RemoveAt(last);
        }
    }
}
