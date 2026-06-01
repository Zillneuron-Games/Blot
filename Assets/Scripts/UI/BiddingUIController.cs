using System.Collections.Generic;
using Blot.Bidding;
using Blot.Cards;
using Blot.Gameplay.Events;
using Blot.Players;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Blot.UI
{
    /// <summary>
    /// Bidding UI panel for the human player.
    ///
    /// Inspector setup required:
    ///   _biddingPanel        — root GameObject to show/hide (the whole panel)
    ///   _bidValueLabel       — shows the current selected bid value
    ///   _decreaseButton      — lowers bid value by 1
    ///   _increaseButton      — raises bid value by 1
    ///   _suitButtons[0..4]   — Clubs / Diamonds / Hearts / Spades / NoTrump
    ///   _passButton          — pass without bidding
    ///   _confirmButton       — confirm selected suit + value
    ///   _currentBidLabel     — "Current bid: 9 Hearts by You"
    ///   _playerBidLabels[0..3] — per-seat latest action labels
    /// </summary>
    public class BiddingUIController : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject _biddingPanel;

        [Header("Bid Value Controls")]
        [SerializeField] private TMP_Text _bidValueLabel;
        [SerializeField] private Button   _decreaseButton;
        [SerializeField] private Button   _increaseButton;

        [Header("Suit Buttons (order: Clubs, Diamonds, Hearts, Spades, NoTrump)")]
        [SerializeField] private Button[] _suitButtons = new Button[5];

        [Header("Action Buttons")]
        [SerializeField] private Button _passButton;
        [SerializeField] private Button _confirmButton;

        [Header("Status Labels")]
        [SerializeField] private TMP_Text _currentBidLabel;
        [SerializeField] private TMP_Text _currentBidderLabel;

        [Header("Per-Player Bid Display (index = Player.Id, 0-3)")]
        [SerializeField] private TMP_Text[] _playerBidLabels = new TMP_Text[4];

        // ------------------------------------------------------------------ state

        private HumanPlayer _human;
        private int         _selectedBidValue;
        private int         _minimumBid;
        private Suit        _selectedSuit = Suit.Clubs;

        private static readonly string[] SuitNames = { "Clubs", "Diamonds", "Hearts", "Spades", "NoTrump" };

        // ------------------------------------------------------------------ lifecycle

        private void Start()
        {
            _human = GameManager.Instance.MatchManager.GetHumanPlayer();
            _human.AutoBidFallback = false;   // this UI is now handling bids

            _human.OnBidRequested    += HandleBidRequested;
            GameEvents.OnBidPlaced   += HandleBidPlaced;
            GameEvents.OnBiddingComplete  += HandleBiddingComplete;
            GameEvents.OnBiddingAllPassed += HandleBiddingAllPassed;
            GameEvents.OnGameRestarted    += HandleGameRestarted;

            // Wire suit buttons.
            for (int i = 0; i < _suitButtons.Length && i < 5; i++)
            {
                int captured = i;
                _suitButtons[i].onClick.AddListener(() => SelectSuit((Suit)captured));
            }

            _decreaseButton.onClick.AddListener(DecreaseBid);
            _increaseButton.onClick.AddListener(IncreaseBid);
            _passButton.onClick.AddListener(OnPassClicked);
            _confirmButton.onClick.AddListener(OnConfirmClicked);

            _biddingPanel.SetActive(false);
            ClearPlayerBidLabels();
        }

        private void OnDestroy()
        {
            if (_human != null)
                _human.OnBidRequested -= HandleBidRequested;

            GameEvents.OnBidPlaced        -= HandleBidPlaced;
            GameEvents.OnBiddingComplete  -= HandleBiddingComplete;
            GameEvents.OnBiddingAllPassed -= HandleBiddingAllPassed;
            GameEvents.OnGameRestarted    -= HandleGameRestarted;
        }

        // ------------------------------------------------------------------ event handlers

        private void HandleBidRequested(int minimumBid)
        {
            _minimumBid        = minimumBid;
            _selectedBidValue  = minimumBid;
            _selectedSuit      = Suit.Clubs;

            RefreshValueLabel();
            UpdateCurrentBidderLabel($"Your turn to bid! Min: {minimumBid}");
            _biddingPanel.SetActive(true);
        }

        private void HandleBidPlaced(Player player, Bid bid)
        {
            if (player.Id < 0 || player.Id >= _playerBidLabels.Length) return;
            if (_playerBidLabels[player.Id] == null) return;

            _playerBidLabels[player.Id].text = bid == null
                ? $"{player.Name}: Pass"
                : $"{player.Name}: {bid}";

            if (bid != null)
                _currentBidLabel.text = $"Current bid: {bid} by {player.Name}";
        }

        private void HandleBiddingComplete(Player winner, Bid bid)
        {
            _biddingPanel.SetActive(false);
            UpdateCurrentBidderLabel($"Contract: {bid} — {winner.Team}");
        }

        private void HandleBiddingAllPassed()
        {
            _biddingPanel.SetActive(false);
            UpdateCurrentBidderLabel("All passed — redealing...");
            ClearPlayerBidLabels();
        }

        private void HandleGameRestarted()
        {
            _biddingPanel.SetActive(false);
            if (_currentBidLabel != null)   _currentBidLabel.text   = string.Empty;
            if (_currentBidderLabel != null) _currentBidderLabel.text = string.Empty;
            ClearPlayerBidLabels();
        }

        // ------------------------------------------------------------------ UI interactions

        private void SelectSuit(Suit suit)
        {
            _selectedSuit = suit;
            // Optional: highlight the selected button visually here.
        }

        private void DecreaseBid()
        {
            if (_selectedBidValue > _minimumBid)
            {
                _selectedBidValue--;
                RefreshValueLabel();
            }
        }

        private void IncreaseBid()
        {
            if (_selectedBidValue < 16)
            {
                _selectedBidValue++;
                RefreshValueLabel();
            }
        }

        private void OnPassClicked()
        {
            _biddingPanel.SetActive(false);
            _human.TryPlaceBid(null);
        }

        private void OnConfirmClicked()
        {
            _biddingPanel.SetActive(false);
            _human.TryPlaceBid(new Bid(_selectedBidValue, _selectedSuit));
        }

        // ------------------------------------------------------------------ helpers

        private void RefreshValueLabel()
        {
            if (_bidValueLabel != null)
                _bidValueLabel.text = _selectedBidValue.ToString();

            if (_decreaseButton != null)
                _decreaseButton.interactable = _selectedBidValue > _minimumBid;

            if (_increaseButton != null)
                _increaseButton.interactable = _selectedBidValue < 16;
        }

        private void UpdateCurrentBidderLabel(string text)
        {
            if (_currentBidderLabel != null)
                _currentBidderLabel.text = text;
        }

        private void ClearPlayerBidLabels()
        {
            foreach (var label in _playerBidLabels)
                if (label != null) label.text = string.Empty;
        }
    }
}
