using Blot.Cards;
using Blot.Gameplay;
using Blot.Gameplay.Events;
using Blot.Players;
using UnityEngine;
using UnityEngine.UI;

namespace Blot.UI
{
    /// <summary>
    /// Displays the four card slots in the centre of the table.
    /// Index in <see cref="_playerSlots"/> matches <see cref="Player.Id"/>:
    ///   0 = You (bottom), 1 = CPU East (right), 2 = CPU North (top), 3 = CPU West (left)
    /// </summary>
    public class TrickAreaView : MonoBehaviour
    {
        [SerializeField] private Image[] _playerSlots = new Image[4];

        // ------------------------------------------------------------------ lifecycle

        private void OnEnable()
        {
            GameEvents.OnCardPlayed     += HandleCardPlayed;
            GameEvents.OnTrickCompleted += HandleTrickCompleted;
            GameEvents.OnCardsDealt     += ClearAllSlots;
        }

        private void OnDisable()
        {
            GameEvents.OnCardPlayed     -= HandleCardPlayed;
            GameEvents.OnTrickCompleted -= HandleTrickCompleted;
            GameEvents.OnCardsDealt     -= ClearAllSlots;
        }

        private void Start() => ClearAllSlots();

        // ------------------------------------------------------------------ handlers

        private void HandleCardPlayed(Player player, Card card)
        {
            if (player.Id < 0 || player.Id >= _playerSlots.Length) return;

            var slot   = _playerSlots[player.Id];
            var sprite = CardSpriteProvider.GetSprite(card);

            if (sprite != null) slot.sprite = sprite;
            slot.color = Color.white;
            slot.gameObject.SetActive(true);
        }

        private void HandleTrickCompleted(Trick _) => ClearAllSlots();

        private void ClearAllSlots()
        {
            foreach (var slot in _playerSlots)
                if (slot != null) slot.gameObject.SetActive(false);
        }
    }
}
