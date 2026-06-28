using System;
using System.Collections;
using Blot.Bidding;
using Blot.Cards;
using Blot.Gameplay;
using Blot.Gameplay.Events;
using Blot.Gameplay.Rules;
using Blot.Players;
using Blot.Scoring;
using TMPro;
using UnityEngine;

namespace Blot.UI
{
    /// <summary>
    /// Presentation-layer MonoBehaviour that paces gameplay so the human player
    /// can read every action.
    ///
    /// Inspector setup:
    ///
    ///   [Delays]
    ///   BiddingActionDelay   — pause (sec) after each bid/pass/challenge action
    ///   CardPlayDelay        — pause (sec) between consecutive card plays
    ///   TrickResultDelay     — pause (sec) to display all 4 cards + trick winner
    ///
    ///   [UI References]
    ///   _eventMessageLabel   — TMP_Text for running game messages (optional)
    ///   _roundResultPanel    — RoundResultPanel shown at round end (optional)
    ///
    /// Null-safe: if this object is not in the scene all delays are skipped and
    /// the round result panel is bypassed — the game runs at full speed.
    /// </summary>
    public class GamePresentationController : MonoBehaviour
    {
        // ------------------------------------------------------------------ singleton
        public static GamePresentationController Instance { get; private set; }

        // ------------------------------------------------------------------ configurable delays
        [Header("Delays (seconds)")]
        [Tooltip("Pause after each bid, pass, challenge, or sure-response action.")]
        public float BiddingActionDelay = 1.0f;

        [Tooltip("Pause after a card is played, before the next player acts.")]
        public float CardPlayDelay = 0.8f;

        [Tooltip("Pause after all 4 cards are visible, before clearing the trick.")]
        public float TrickResultDelay = 2.0f;

        // ------------------------------------------------------------------ UI references
        [Header("UI References")]
        [SerializeField] private TMP_Text       _eventMessageLabel;
        [SerializeField] private RoundResultPanel _roundResultPanel;

        // ------------------------------------------------------------------ lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnEnable()
        {
            GameEvents.OnBidPlaced           += HandleBidPlaced;
            GameEvents.OnChallenged          += HandleChallenged;
            GameEvents.OnChallengeResponded  += HandleChallengeResponded;
            GameEvents.OnBiddingComplete     += HandleBiddingComplete;
            GameEvents.OnBiddingAllPassed    += HandleBiddingAllPassed;
            GameEvents.OnCardPlayed          += HandleCardPlayed;
            GameEvents.OnTrickCompleted      += HandleTrickCompleted;
            GameEvents.OnBeloteAnnounced     += HandleBeloteAnnounced;
            GameEvents.OnGameRestarted       += HandleGameRestarted;
        }

        private void OnDisable()
        {
            GameEvents.OnBidPlaced           -= HandleBidPlaced;
            GameEvents.OnChallenged          -= HandleChallenged;
            GameEvents.OnChallengeResponded  -= HandleChallengeResponded;
            GameEvents.OnBiddingComplete     -= HandleBiddingComplete;
            GameEvents.OnBiddingAllPassed    -= HandleBiddingAllPassed;
            GameEvents.OnCardPlayed          -= HandleCardPlayed;
            GameEvents.OnTrickCompleted      -= HandleTrickCompleted;
            GameEvents.OnBeloteAnnounced     -= HandleBeloteAnnounced;
            GameEvents.OnGameRestarted       -= HandleGameRestarted;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ------------------------------------------------------------------ coroutine dispatch

        /// <summary>
        /// Runs <paramref name="action"/> after <paramref name="delay"/> seconds.
        /// If delay is 0 the action runs on the next frame to avoid deep call stacks.
        /// </summary>
        public void RunAfterDelay(float delay, Action action)
        {
            Debug.Log($"[Pacing] Scheduling action in {delay:F2}s");
            StartCoroutine(DelayCoroutine(delay, action));
        }

        private static IEnumerator DelayCoroutine(float delay, Action action)
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);
            else
                yield return null;   // one frame, prevents deep synchronous call stacks
            action?.Invoke();
        }

        // ------------------------------------------------------------------ round result panel

        /// <summary>
        /// Shows the round result panel and invokes <paramref name="onContinue"/>
        /// when the human clicks Continue.  If no panel is assigned the callback
        /// fires immediately on the next frame.
        /// </summary>
        public void ShowRoundResult(RoundEndData data, Action onContinue)
        {
            // Use the Inspector-assigned reference; fall back to scene lookup if null.
            var panel = _roundResultPanel != null
                ? _roundResultPanel
                : FindFirstObjectByType<RoundResultPanel>(FindObjectsInactive.Include);

            if (panel != null)
            {
                Debug.Log("[Round Result] Showing panel");
                panel.Show(data, onContinue);
            }
            else
            {
                Debug.LogWarning("[Round Result] No RoundResultPanel found in scene — skipping.");
                RunAfterDelay(0f, onContinue);
            }
        }

        // ------------------------------------------------------------------ message display

        public void ShowMessage(string message)
        {
            if (_eventMessageLabel != null)
                _eventMessageLabel.text = message;
        }

        // ------------------------------------------------------------------ game event handlers

        private void HandleBidPlaced(Player player, Bid bid)
        {
            string msg;
            if (bid == null)
                msg = $"{player.Name}:  Pass";
            else if (bid.IsChallenge)
                msg = $"{player.Name}:  I Don't Believe!";
            else
                msg = $"{player.Name}:  {bid}";

            Debug.Log($"[Pacing] Showing bid action: {msg}");
            ShowMessage(msg);
        }

        private void HandleChallenged(Player challenger)
        {
            ShowMessage($"{challenger.Name} challenges!");
        }

        private void HandleChallengeResponded(Player responder, bool isSure)
        {
            ShowMessage(isSure
                ? $"{responder.Name}:  I'm Sure!"
                : $"{responder.Name}:  Pass (no confirmation)");
        }

        private void HandleBiddingComplete(Player winner, Bid bid)
        {
            ShowMessage($"Contract:  {bid}  —  {winner.Team}  ({winner.Name})");
        }

        private void HandleBiddingAllPassed()
        {
            ShowMessage("All players passed.  Redealing...");
        }

        private void HandleCardPlayed(Player player, Card card)
        {
            string msg = $"{player.Name} played  {card}";
            Debug.Log($"[Pacing] Waiting after card play: {msg}");
            ShowMessage(msg);
        }

        private void HandleTrickCompleted(Trick trick)
        {
            var winner = trick.GetWinner();
            string msg = winner != null
                ? $"Trick winner:  {winner.Name}"
                : "Trick complete";
            Debug.Log($"[Pacing] Showing trick result: {msg}");
            ShowMessage(msg);
        }

        private void HandleBeloteAnnounced(Player player, BeloteEvent evt)
        {
            if (evt == BeloteEvent.Belote)
                ShowMessage($"{player.Name}:  Belote!");
            else if (evt == BeloteEvent.Rebelote)
                ShowMessage($"{player.Name}:  Rebelote!  +20 pts for {player.Team}");
        }

        private void HandleGameRestarted()
        {
            ShowMessage("New match — good luck!");
        }
    }
}
