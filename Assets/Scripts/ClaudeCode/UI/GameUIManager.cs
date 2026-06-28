using Blot.Cards;
using Blot.Core.Managers;
using Blot.Gameplay;
using Blot.Gameplay.Events;
using Blot.Players;
using Blot.Scoring;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Blot.UI
{
    /// <summary>
    /// Subscribes to <see cref="GameEvents"/> and updates all HUD elements.
    /// Contains no game logic — pure presentation layer.
    /// </summary>
    public class GameUIManager : MonoBehaviour
    {
        [Header("HUD")]
        [SerializeField] private TMP_Text _trumpLabel;
        [SerializeField] private TMP_Text _scoreLabel;
        [SerializeField] private TMP_Text _trickInfoLabel;
        [SerializeField] private TMP_Text _statusLabel;

        [Header("End Screen")]
        [SerializeField] private GameObject _endPanel;
        [SerializeField] private TMP_Text   _endResultLabel;
        [SerializeField] private Button     _restartButton;

        [Header("Hand")]
        [SerializeField] private PlayerHandView _humanHandView;

        // ------------------------------------------------------------------ lifecycle

        private void Start()
        {
            SubscribeToEvents();

            _endPanel.SetActive(false);

            var human = GameManager.Instance.MatchManager.GetHumanPlayer();
            _humanHandView.Bind(human);

            _restartButton.onClick.AddListener(() =>
            {
                _endPanel.SetActive(false);
                GameManager.Instance.RestartMatch();
            });
        }

        private void OnDestroy() => UnsubscribeFromEvents();

        // ------------------------------------------------------------------ event wiring

        private void SubscribeToEvents()
        {
            GameEvents.OnCardsDealt        += HandleCardsDealt;
            GameEvents.OnTrumpSelected     += HandleTrumpSelected;
            GameEvents.OnPlayerTurnStarted += HandlePlayerTurnStarted;
            GameEvents.OnCardPlayed        += HandleCardPlayed;
            GameEvents.OnTrickCompleted    += HandleTrickCompleted;
            GameEvents.OnRoundEnded        += HandleRoundEnded;
            GameEvents.OnMatchEnded        += HandleMatchEnded;
            GameEvents.OnGameRestarted     += HandleGameRestarted;
        }

        private void UnsubscribeFromEvents()
        {
            GameEvents.OnCardsDealt        -= HandleCardsDealt;
            GameEvents.OnTrumpSelected     -= HandleTrumpSelected;
            GameEvents.OnPlayerTurnStarted -= HandlePlayerTurnStarted;
            GameEvents.OnCardPlayed        -= HandleCardPlayed;
            GameEvents.OnTrickCompleted    -= HandleTrickCompleted;
            GameEvents.OnRoundEnded        -= HandleRoundEnded;
            GameEvents.OnMatchEnded        -= HandleMatchEnded;
            GameEvents.OnGameRestarted     -= HandleGameRestarted;
        }

        // ------------------------------------------------------------------ handlers

        private void HandleCardsDealt()
        {
            _humanHandView.RebuildHand();
            _statusLabel.text    = "Cards dealt.";
            _trickInfoLabel.text = string.Empty;
        }

        private void HandleTrumpSelected(Suit trump)
        {
            _trumpLabel.text  = $"Trump: {trump}";
            _statusLabel.text = $"Trump is {trump}!";
        }

        private void HandlePlayerTurnStarted(Player player) =>
            _statusLabel.text = $"{player.Name}'s turn...";

        private void HandleCardPlayed(Player player, Card card) =>
            _trickInfoLabel.text += $"\n{player.Name}: {card}";

        private void HandleTrickCompleted(Trick trick)
        {
            var winner = trick.GetWinner();
            _statusLabel.text    = $"{winner.Name} wins the trick!  (+{trick.GetTotalPoints()} pts)";
            _trickInfoLabel.text = string.Empty;
        }

        private void HandleRoundEnded(RoundResult result)
        {
            RefreshScoreLabel(result.TeamAMatchTotal, result.TeamBMatchTotal);
            _statusLabel.text = $"Round over — {result.Winner} wins  "
                              + $"(TeamA {result.TeamAPoints} | TeamB {result.TeamBPoints})";
        }

        private void HandleMatchEnded(TeamId winner)
        {
            _endResultLabel.text = winner == TeamId.TeamA ? "You won the match!" : "CPU won the match!";
            _endPanel.SetActive(true);
        }

        private void HandleGameRestarted()
        {
            RefreshScoreLabel(0, 0);
            _trumpLabel.text     = string.Empty;
            _trickInfoLabel.text = string.Empty;
            _statusLabel.text    = "New match — good luck!";
        }

        private void RefreshScoreLabel(int a, int b) =>
            _scoreLabel.text = $"TeamA  {a}  |  TeamB  {b}     (first to {ScoreManager.WinTarget})";
    }
}
