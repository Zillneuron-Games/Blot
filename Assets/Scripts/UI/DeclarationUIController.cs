using System.Collections.Generic;
using Blot.Declarations;
using Blot.Gameplay.Events;
using Blot.Players;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Blot.UI
{
    /// <summary>
    /// Handles the human player's declaration UI:
    ///   — Announcement panel: shown before trick 1, lists detected declarations
    ///   — Reveal panel: shown at the reveal window (trick 2 / trick 3)
    ///
    /// Inspector setup required:
    ///   _announcePanelRoot      — root of the announcement panel
    ///   _declListParent         — layout group parent for _declRowPrefab instances
    ///   _declRowPrefab          — prefab with Toggle + TMP_Text (label) + TMP_Text (value)
    ///   _announceConfirmButton  — "Announce" button (announces selected)
    ///   _announceSkipButton     — "Skip" button (announces nothing)
    ///   _announceSummaryLabel   — shows "X declaration(s) selected, Y pts"
    ///
    ///   _revealPanelRoot        — root of the reveal panel
    ///   _revealDeclLabel        — lists what the player announced and must reveal
    ///   _revealShowButton       — "Show" button (confirms reveal)
    ///   _revealSkipButton       — "Skip" button (forfeits reveal)
    ///
    ///   _declStatusLabel        — shows each player's latest announcement (all 4 rows)
    ///   _winnerLabel            — shows "Winner team: TeamA / None"
    /// </summary>
    public class DeclarationUIController : MonoBehaviour
    {
        // ------------------------------------------------------------------ Inspector refs

        [Header("Announcement Panel")]
        [SerializeField] private GameObject _announcePanelRoot;
        [SerializeField] private Transform  _declListParent;
        [SerializeField] private GameObject _declRowPrefab;
        [SerializeField] private Button     _announceConfirmButton;
        [SerializeField] private Button     _announceSkipButton;
        [SerializeField] private TMP_Text   _announceSummaryLabel;

        [Header("Reveal Panel")]
        [SerializeField] private GameObject _revealPanelRoot;
        [SerializeField] private TMP_Text   _revealDeclLabel;
        [SerializeField] private Button     _revealShowButton;
        [SerializeField] private Button     _revealSkipButton;

        [Header("Status Display")]
        [SerializeField] private TMP_Text   _winnerLabel;
        [SerializeField] private TMP_Text[] _playerDeclLabels = new TMP_Text[4];

        // ------------------------------------------------------------------ State

        private HumanPlayer          _human;
        private List<Declaration>    _available     = new();
        private List<Declaration>    _selected      = new();
        private List<Toggle>         _rowToggles    = new();
        private List<Declaration>    _pendingReveal = new();

        // ------------------------------------------------------------------ Lifecycle

        private void Start()
        {
            _human = GameManager.Instance.MatchManager.GetHumanPlayer();
            _human.AutoDeclareFallback = false;   // this UI is now handling declarations

            _human.OnDeclareRequested += HandleDeclareRequested;
            _human.OnRevealRequested  += HandleRevealRequested;

            GameEvents.OnDeclarationsAnnounced     += HandleDeclarationsAnnounced;
            GameEvents.OnDeclarationWinnerDetermined += HandleWinnerDetermined;
            GameEvents.OnDeclarationsRevealed      += HandleDeclarationsRevealed;
            GameEvents.OnGameRestarted             += HandleGameRestarted;

            SafeSetActive(_announcePanelRoot, false);
            SafeSetActive(_revealPanelRoot,   false);

            if (_announceConfirmButton != null)
                _announceConfirmButton.onClick.AddListener(OnAnnounceConfirm);
            if (_announceSkipButton != null)
                _announceSkipButton.onClick.AddListener(OnAnnounceSkip);
            if (_revealShowButton != null)
                _revealShowButton.onClick.AddListener(OnRevealShow);
            if (_revealSkipButton != null)
                _revealSkipButton.onClick.AddListener(OnRevealSkip);

            ClearStatusLabels();
        }

        private void OnDestroy()
        {
            if (_human != null)
            {
                _human.OnDeclareRequested -= HandleDeclareRequested;
                _human.OnRevealRequested  -= HandleRevealRequested;
            }
            GameEvents.OnDeclarationsAnnounced      -= HandleDeclarationsAnnounced;
            GameEvents.OnDeclarationWinnerDetermined -= HandleWinnerDetermined;
            GameEvents.OnDeclarationsRevealed       -= HandleDeclarationsRevealed;
            GameEvents.OnGameRestarted              -= HandleGameRestarted;
        }

        // ------------------------------------------------------------------ Announcement panel

        private void HandleDeclareRequested(List<Declaration> available)
        {
            // Guard: if no declarations are available, skip the panel entirely.
            if (available == null || available.Count == 0)
            {
                Debug.Log("[Declarations] Human has no declarations, skipping declaration UI");
                _human.TryAnnounce(new List<Declaration>());
                return;
            }

            _available = available;
            _selected.Clear();

            BuildDeclRows(available);
            RefreshSummaryLabel();
            SafeSetActive(_announcePanelRoot, true);
        }

        private void BuildDeclRows(List<Declaration> declarations)
        {
            _rowToggles.Clear();

            if (_declListParent != null)
                foreach (Transform child in _declListParent)
                    Destroy(child.gameObject);

            if (declarations.Count == 0)
            {
                if (_announceSummaryLabel != null)
                    _announceSummaryLabel.text = "No declarations in hand.";
                return;
            }

            if (_declRowPrefab == null || _declListParent == null) return;

            var trump = GameManager.Instance.RoundManager.Trump;

            for (int i = 0; i < declarations.Count; i++)
            {
                var decl   = declarations[i];
                var rowObj = Instantiate(_declRowPrefab, _declListParent);
                var labels = rowObj.GetComponentsInChildren<TMP_Text>();
                var toggle = rowObj.GetComponentInChildren<Toggle>();

                if (labels.Length >= 1) labels[0].text = decl.ToString();
                if (labels.Length >= 2) labels[1].text = $"({decl.GetValue(trump)} pts, rank {decl.GetStrengthRank(trump)})";

                if (toggle != null)
                {
                    int captured = i;
                    toggle.isOn = false;
                    toggle.onValueChanged.AddListener(on =>
                    {
                        if (on) AddSelection(captured);
                        else    RemoveSelection(captured);
                    });
                    _rowToggles.Add(toggle);
                }
            }
        }

        private void AddSelection(int index)
        {
            var decl = _available[index];

            // Validate non-overlap before adding.
            var testSet = new List<Declaration>(_selected) { decl };
            if (!DeclarationDetector.IsNonOverlapping(testSet))
            {
                // Revert the toggle.
                if (index < _rowToggles.Count)
                    _rowToggles[index].SetIsOnWithoutNotify(false);
                return;
            }

            _selected.Add(decl);
            RefreshSummaryLabel();
        }

        private void RemoveSelection(int index)
        {
            _selected.Remove(_available[index]);
            RefreshSummaryLabel();
        }

        private void RefreshSummaryLabel()
        {
            if (_announceSummaryLabel == null) return;

            int total = 0;
            foreach (var d in _selected)
                total += d.GetValue(GameManager.Instance.RoundManager.Trump);

            _announceSummaryLabel.text = $"{_selected.Count} selected — {total} pts";
        }

        private void OnAnnounceConfirm()
        {
            SafeSetActive(_announcePanelRoot, false);
            _human.TryAnnounce(new List<Declaration>(_selected));
        }

        private void OnAnnounceSkip()
        {
            SafeSetActive(_announcePanelRoot, false);
            _human.TryAnnounce(new List<Declaration>());
        }

        // ------------------------------------------------------------------ Reveal panel

        private void HandleRevealRequested(List<Declaration> toReveal)
        {
            _pendingReveal = toReveal;

            if (_revealDeclLabel != null)
            {
                var sb = new System.Text.StringBuilder("Reveal declarations:\n");
                foreach (var d in toReveal) sb.AppendLine($"  • {d}");
                _revealDeclLabel.text = sb.ToString();
            }

            SafeSetActive(_revealPanelRoot, true);
        }

        private void OnRevealShow()
        {
            SafeSetActive(_revealPanelRoot, false);
            _human.TryReveal(true);
        }

        private void OnRevealSkip()
        {
            SafeSetActive(_revealPanelRoot, false);
            _human.TryReveal(false);
        }

        // ------------------------------------------------------------------ Status display

        private void HandleDeclarationsAnnounced(Player player, List<Declaration> decls)
        {
            if (player.Id < 0 || player.Id >= _playerDeclLabels.Length) return;
            if (_playerDeclLabels[player.Id] == null) return;

            if (decls == null || decls.Count == 0)
            {
                _playerDeclLabels[player.Id].text = $"{player.Name}: (no declarations)";
                return;
            }

            var sb = new System.Text.StringBuilder($"{player.Name}:");
            foreach (var d in decls) sb.Append($" {d}");
            _playerDeclLabels[player.Id].text = sb.ToString();
        }

        private void HandleWinnerDetermined(TeamId? winner)
        {
            if (_winnerLabel == null) return;
            _winnerLabel.text = winner.HasValue
                ? $"Declaration winner: {winner}"
                : "No declarations.";
        }

        private void HandleDeclarationsRevealed(Player player, List<Declaration> decls)
        {
            if (player.Id < 0 || player.Id >= _playerDeclLabels.Length) return;
            if (_playerDeclLabels[player.Id] == null) return;

            _playerDeclLabels[player.Id].text += " [REVEALED]";
        }

        private void HandleGameRestarted()
        {
            SafeSetActive(_announcePanelRoot, false);
            SafeSetActive(_revealPanelRoot,   false);
            if (_winnerLabel != null) _winnerLabel.text = string.Empty;
            ClearStatusLabels();
        }

        // ------------------------------------------------------------------ Helpers

        private void ClearStatusLabels()
        {
            foreach (var label in _playerDeclLabels)
                if (label != null) label.text = string.Empty;
        }

        private static void SafeSetActive(GameObject obj, bool active)
        {
            if (obj != null) obj.SetActive(active);
        }
    }
}
