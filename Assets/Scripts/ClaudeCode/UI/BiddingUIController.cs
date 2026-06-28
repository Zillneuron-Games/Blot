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
    /// Bidding UI for the human player.
    ///
    /// Inspector setup:
    ///
    ///   [Normal Bid Panel]
    ///   _biddingPanel         — root panel (show/hide)
    ///   _normalBidSubPanel    — sub-panel for normal bid controls (hidden in Kaput mode)
    ///   _bidValueLabel        — current selected value
    ///   _decreaseButton       — value - 1
    ///   _increaseButton       — value + 1
    ///   _suitButtons[0..4]    — Clubs / Diamonds / Hearts / Spades / NoTrump
    ///   _passButton           — pass this turn
    ///   _confirmButton        — confirm bid
    ///   _challengeButton      — "I Don't Believe!" (shown only when legal)
    ///
    ///   [Kaput Options]
    ///   _kaputButton          — toggles Kaput mode on/off (hidden when Kaput forced)
    ///   _kaputSubPanel        — sub-panel for Kaput controls (hidden in normal mode)
    ///   _kaputModeLabel       — e.g. "KAPUT" heading text
    ///   _kaputExtraLabel      — shows "+0", "+1" etc.
    ///   _kaputExtraDecButton  — extra value - 1
    ///   _kaputExtraIncButton  — extra value + 1
    ///
    ///   [Sure-Response Panel]
    ///   _sureResponsePanel    — shown when human must respond to a challenge
    ///   _sureButton           — "I'm Sure"
    ///   _surePassButton       — "Pass" (decline to confirm)
    ///
    ///   [Status Labels]
    ///   _currentBidLabel      — e.g. "Current bid: 9 Hearts by CPU East"
    ///   _currentBidderLabel   — e.g. "Your turn to bid! Min: 9"
    ///   _challengeStatusLabel — shows live challenge events
    ///   _playerBidLabels[0..3]— per-seat latest action
    /// </summary>
    public class BiddingUIController : MonoBehaviour
    {
        [Header("Normal Bid Panel")]
        [SerializeField] private GameObject _biddingPanel;
        [SerializeField] private GameObject _normalBidSubPanel;   // hide in Kaput mode
        [SerializeField] private TMP_Text   _bidValueLabel;
        [SerializeField] private Button     _decreaseButton;
        [SerializeField] private Button     _increaseButton;

        [Header("Suit Buttons (order: Clubs, Diamonds, Hearts, Spades, NoTrump)")]
        [SerializeField] private Button[] _suitButtons = new Button[5];

        [Header("Action Buttons (inside bid panel)")]
        [SerializeField] private Button _passButton;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _challengeButton;   // "I Don't Believe"

        [Header("Kaput Options")]
        [SerializeField] private Button     _kaputButton;          // toggles Kaput mode
        [SerializeField] private GameObject _kaputSubPanel;        // show in Kaput mode
        [SerializeField] private TMP_Text   _kaputModeLabel;       // "KAPUT" heading
        [SerializeField] private TMP_Text   _kaputExtraLabel;      // "+0", "+1" etc.
        [SerializeField] private Button     _kaputExtraDecButton;  // extra - 1
        [SerializeField] private Button     _kaputExtraIncButton;  // extra + 1

        [Header("Sure Response Panel")]
        [SerializeField] private GameObject _sureResponsePanel;
        [SerializeField] private Button     _sureButton;       // "I'm Sure"
        [SerializeField] private Button     _surePassButton;   // "Pass" / decline

        [Header("Status Labels")]
        [SerializeField] private TMP_Text   _currentBidLabel;
        [SerializeField] private TMP_Text   _currentBidderLabel;
        [SerializeField] private TMP_Text   _challengeStatusLabel;

        [Header("Per-Player Bid Display (index = Player.Id, 0-3)")]
        [SerializeField] private TMP_Text[] _playerBidLabels = new TMP_Text[4];

        // ------------------------------------------------------------------ state

        private HumanPlayer _human;
        private int         _selectedBidValue;
        private int         _minimumBid;
        private Suit        _selectedSuit = Suit.Clubs;

        private bool _kaputMode;
        private int  _kaputExtra;
        private int  _kaputMinExtra;
        private bool _onlyKaputAllowed;   // true when current bid is Kaput — normal mode blocked

        // ------------------------------------------------------------------ lifecycle

        private void Start()
        {
            _human = GameManager.Instance.MatchManager.GetHumanPlayer();
            _human.AutoBidFallback = false;

            // Player events.
            _human.OnBidRequested          += HandleBidRequested;
            _human.OnSureResponseRequested += HandleSureResponseRequested;

            // Game events.
            GameEvents.OnBidPlaced           += HandleBidPlaced;
            GameEvents.OnBiddingComplete     += HandleBiddingComplete;
            GameEvents.OnBiddingAllPassed    += HandleBiddingAllPassed;
            GameEvents.OnChallenged          += HandleChallenged;
            GameEvents.OnChallengeResponded  += HandleChallengeResponded;
            GameEvents.OnChallengeBonus      += HandleChallengeBonus;
            GameEvents.OnGameRestarted       += HandleGameRestarted;

            // Suit buttons.
            for (int i = 0; i < _suitButtons.Length && i < 5; i++)
            {
                int captured = i;
                if (_suitButtons[i] != null)
                    _suitButtons[i].onClick.AddListener(() => SelectSuit((Suit)captured));
            }

            if (_decreaseButton       != null) _decreaseButton      .onClick.AddListener(DecreaseBid);
            if (_increaseButton       != null) _increaseButton      .onClick.AddListener(IncreaseBid);
            if (_passButton           != null) _passButton          .onClick.AddListener(OnPassClicked);
            if (_confirmButton        != null) _confirmButton       .onClick.AddListener(OnConfirmClicked);
            if (_challengeButton      != null) _challengeButton     .onClick.AddListener(OnChallengeClicked);
            if (_sureButton           != null) _sureButton          .onClick.AddListener(OnSureClicked);
            if (_surePassButton       != null) _surePassButton      .onClick.AddListener(OnSurePassClicked);
            if (_kaputButton          != null) _kaputButton         .onClick.AddListener(OnKaputToggleClicked);
            if (_kaputExtraDecButton  != null) _kaputExtraDecButton .onClick.AddListener(DecreaseKaputExtra);
            if (_kaputExtraIncButton  != null) _kaputExtraIncButton .onClick.AddListener(IncreaseKaputExtra);

            SafeSetActive(_biddingPanel,      false);
            SafeSetActive(_sureResponsePanel, false);
            ClearPlayerBidLabels();
        }

        private void OnDestroy()
        {
            if (_human != null)
            {
                _human.OnBidRequested          -= HandleBidRequested;
                _human.OnSureResponseRequested -= HandleSureResponseRequested;
            }
            GameEvents.OnBidPlaced           -= HandleBidPlaced;
            GameEvents.OnBiddingComplete     -= HandleBiddingComplete;
            GameEvents.OnBiddingAllPassed    -= HandleBiddingAllPassed;
            GameEvents.OnChallenged          -= HandleChallenged;
            GameEvents.OnChallengeResponded  -= HandleChallengeResponded;
            GameEvents.OnChallengeBonus      -= HandleChallengeBonus;
            GameEvents.OnGameRestarted       -= HandleGameRestarted;
        }

        // ------------------------------------------------------------------ normal bid panel

        private void HandleBidRequested(int minimumBid)
        {
            _minimumBid       = minimumBid;
            _selectedBidValue = Mathf.Min(minimumBid, 16);
            _selectedSuit     = Suit.Clubs;

            // minimumBid > 16 means the current bid is Kaput — only Kaput bids are valid.
            _onlyKaputAllowed = _human.OnlyKaputBidAllowed;
            _kaputMinExtra    = _onlyKaputAllowed ? minimumBid - 25 : 0;
            _kaputExtra       = _kaputMinExtra;
            _kaputMode        = _onlyKaputAllowed;   // force Kaput mode if required

            // Show/hide challenge button based on legality this turn.
            if (_challengeButton != null)
                _challengeButton.gameObject.SetActive(_human.CanChallengeNow);

            // Show/hide Kaput toggle: only when Kaput mode is not forced.
            if (_kaputButton != null)
                _kaputButton.gameObject.SetActive(!_onlyKaputAllowed);

            RefreshBidMode();
            UpdateBidderLabel(_onlyKaputAllowed
                ? $"Your turn to bid! Kaput only (min +{_kaputMinExtra})"
                : $"Your turn to bid! Min: {minimumBid}");
            SafeSetActive(_biddingPanel, true);
        }

        private void SelectSuit(Suit suit) => _selectedSuit = suit;

        private void DecreaseBid()
        {
            if (_selectedBidValue > _minimumBid) { _selectedBidValue--; RefreshValueLabel(); }
        }

        private void IncreaseBid()
        {
            if (_selectedBidValue < 16) { _selectedBidValue++; RefreshValueLabel(); }
        }

        // ------------------------------------------------------------------ Kaput controls

        private void OnKaputToggleClicked()
        {
            _kaputMode = !_kaputMode;
            if (_kaputMode && _kaputExtra < _kaputMinExtra)
                _kaputExtra = _kaputMinExtra;
            RefreshBidMode();
        }

        private void DecreaseKaputExtra()
        {
            if (_kaputExtra > _kaputMinExtra) { _kaputExtra--; RefreshKaputExtraLabel(); }
        }

        private void IncreaseKaputExtra()
        {
            _kaputExtra++;
            RefreshKaputExtraLabel();
        }

        // ------------------------------------------------------------------ confirm / pass

        private void OnPassClicked()
        {
            SafeSetActive(_biddingPanel, false);
            _human.TryPlaceBid(null);
        }

        private void OnConfirmClicked()
        {
            SafeSetActive(_biddingPanel, false);
            if (_kaputMode)
                _human.TryPlaceBid(Bid.MakeKaput(_selectedSuit, _kaputExtra));
            else
                _human.TryPlaceBid(new Bid(_selectedBidValue, _selectedSuit));
        }

        private void OnChallengeClicked()
        {
            SafeSetActive(_biddingPanel, false);
            _human.TryPlaceBid(Bid.MakeChallenge());
        }

        // ------------------------------------------------------------------ sure-response panel

        private void HandleSureResponseRequested()
        {
            UpdateChallengeStatus("Opponent challenged your contract! Respond:");
            SafeSetActive(_sureResponsePanel, true);
        }

        private void OnSureClicked()
        {
            SafeSetActive(_sureResponsePanel, false);
            _human.TrySureResponse(true);
        }

        private void OnSurePassClicked()
        {
            SafeSetActive(_sureResponsePanel, false);
            _human.TrySureResponse(false);
        }

        // ------------------------------------------------------------------ game event handlers

        private void HandleBidPlaced(Player player, Bid bid)
        {
            if (player.Id < 0 || player.Id >= _playerBidLabels.Length) return;
            if (_playerBidLabels[player.Id] == null) return;

            string label = bid == null            ? $"{player.Name}: Pass"
                         : bid.IsChallenge        ? $"{player.Name}: I Don't Believe!"
                         : bid.IsKaput            ? $"{player.Name}: {bid}"
                                                  : $"{player.Name}: {bid}";

            _playerBidLabels[player.Id].text = label;

            if (bid != null && !bid.IsChallenge && _currentBidLabel != null)
                _currentBidLabel.text = $"Current bid: {bid} by {player.Name}";
        }

        private void HandleBiddingComplete(Player winner, Bid bid)
        {
            SafeSetActive(_biddingPanel, false);
            UpdateBidderLabel($"Contract: {bid} — {winner.Team}");
        }

        private void HandleBiddingAllPassed()
        {
            SafeSetActive(_biddingPanel, false);
            UpdateBidderLabel("All passed — redealing...");
            ClearPlayerBidLabels();
        }

        private void HandleChallenged(Player challenger)
        {
            UpdateChallengeStatus($"{challenger.Name} says 'I Don't Believe!'");
            if (player_label_valid(challenger))
                _playerBidLabels[challenger.Id].text = $"{challenger.Name}: I Don't Believe!";
        }

        private void HandleChallengeResponded(Player responder, bool isSure)
        {
            string response = isSure ? "I'm Sure!" : "Pass (no confirmation)";
            UpdateChallengeStatus($"{responder.Name}: {response}");
            if (player_label_valid(responder))
                _playerBidLabels[responder.Id].text += $" → {response}";
        }

        private void HandleChallengeBonus(TeamId team, int bonus)
        {
            UpdateChallengeStatus($"Challenge bonus: {team} +{bonus} pts");
        }

        private void HandleGameRestarted()
        {
            SafeSetActive(_biddingPanel,      false);
            SafeSetActive(_sureResponsePanel, false);
            if (_currentBidLabel      != null) _currentBidLabel.text      = string.Empty;
            if (_currentBidderLabel   != null) _currentBidderLabel.text   = string.Empty;
            if (_challengeStatusLabel != null) _challengeStatusLabel.text = string.Empty;
            ClearPlayerBidLabels();
        }

        // ------------------------------------------------------------------ UI helpers

        private void RefreshBidMode()
        {
            SafeSetActive(_normalBidSubPanel, !_kaputMode);
            SafeSetActive(_kaputSubPanel,      _kaputMode);

            if (!_kaputMode)
                RefreshValueLabel();
            else
                RefreshKaputExtraLabel();
        }

        private void RefreshValueLabel()
        {
            if (_bidValueLabel  != null) _bidValueLabel.text = _selectedBidValue.ToString();
            if (_decreaseButton != null) _decreaseButton.interactable = _selectedBidValue > _minimumBid;
            if (_increaseButton != null) _increaseButton.interactable = _selectedBidValue < 16;
        }

        private void RefreshKaputExtraLabel()
        {
            if (_kaputExtraLabel    != null)
                _kaputExtraLabel.text = _kaputExtra > 0 ? $"+{_kaputExtra}" : "+0";
            if (_kaputExtraDecButton != null)
                _kaputExtraDecButton.interactable = _kaputExtra > _kaputMinExtra;
        }

        private void UpdateBidderLabel(string text)
        {
            if (_currentBidderLabel != null) _currentBidderLabel.text = text;
        }

        private void UpdateChallengeStatus(string text)
        {
            if (_challengeStatusLabel != null) _challengeStatusLabel.text = text;
        }

        private void ClearPlayerBidLabels()
        {
            foreach (var label in _playerBidLabels)
                if (label != null) label.text = string.Empty;
        }

        private bool player_label_valid(Player p) =>
            p.Id >= 0 && p.Id < _playerBidLabels.Length && _playerBidLabels[p.Id] != null;

        private static void SafeSetActive(GameObject obj, bool active)
        {
            if (obj != null) obj.SetActive(active);
        }
    }
}
