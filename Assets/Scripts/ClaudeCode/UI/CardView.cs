using Blot.Cards;
using Blot.Players;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Blot.UI
{
    /// <summary>
    /// Represents one card button in the human player's hand.
    /// Bind it via <see cref="Bind"/> then toggle interactability with <see cref="SetInteractable"/>.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class CardView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _labelText;
        [SerializeField] private Image    _cardImage;
        [SerializeField] private Color    _validColor   = Color.white;
        [SerializeField] private Color    _invalidColor = Color.gray;

        /// <summary>The card data this view currently represents.</summary>
        public Card Card => _card;

        private Card        _card;
        private HumanPlayer _owner;
        private Button      _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnClicked);
        }

        public void Bind(Card card, HumanPlayer owner)
        {
            _card           = card;
            _owner          = owner;
            _labelText.text = card.ToString();

            var sprite = CardSpriteProvider.GetSprite(card);
            if (sprite != null) _cardImage.sprite = sprite;

            SetInteractable(false);
        }

        public void SetInteractable(bool canPlay)
        {
            _button.interactable = canPlay;
            _cardImage.color     = canPlay ? _validColor : _invalidColor;
        }

        private void OnClicked() => _owner.TrySelectCard(_card);
    }
}
