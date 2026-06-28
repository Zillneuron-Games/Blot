using System;
using Blot.Cards;
using Blot.Players;
using Blot.Scoring;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Blot.UI
{
    /// <summary>
    /// Round result window. Shown after every round; gameplay is paused until
    /// the human clicks Continue.
    ///
    /// Inspector setup (all fields optional — missing labels are silently skipped):
    ///
    ///   [Contract info]
    ///   _contractTeamLabel    — e.g. "Contract: TeamA"
    ///   _contractPlayerLabel  — e.g. "Player: You"
    ///   _trumpLabel           — e.g. "Trump: Hearts" / "NoTrump" / "Kaput Hearts"
    ///   _bidValueLabel        — e.g. "Bid: 12   Target: 120"
    ///
    ///   [Score breakdown]
    ///   _contractStatusLabel  — "CONTRACT MET" / "CONTRACT FAILED"
    ///   _teamAScoreLabel      — points TeamA earned this round
    ///   _teamBScoreLabel      — points TeamB earned this round
    ///   _teamADeclLabel       — TeamA declaration bonus (raw pts)
    ///   _teamBDeclLabel       — TeamB declaration bonus (raw pts)
    ///   _challengeBonusLabel  — challenge bonus info (hidden if none)
    ///
    ///   [Match totals]
    ///   _teamAMatchLabel      — e.g. "TeamA total: 47"
    ///   _teamBMatchLabel      — e.g. "TeamB total: 31"
    ///
    ///   [Actions]
    ///   _continueButton       — advances to next round / match end
    /// </summary>
    public class RoundResultPanel : MonoBehaviour
    {
        [Header("Contract Info")]
        [SerializeField] private TMP_Text _contractTeamLabel;
        [SerializeField] private TMP_Text _contractPlayerLabel;
        [SerializeField] private TMP_Text _trumpLabel;
        [SerializeField] private TMP_Text _bidValueLabel;

        [Header("Score Breakdown")]
        [SerializeField] private TMP_Text _contractStatusLabel;
        [SerializeField] private TMP_Text _teamAScoreLabel;
        [SerializeField] private TMP_Text _teamBScoreLabel;
        [SerializeField] private TMP_Text _teamADeclLabel;
        [SerializeField] private TMP_Text _teamBDeclLabel;
        [SerializeField] private TMP_Text _challengeBonusLabel;

        [Header("Match Totals")]
        [SerializeField] private TMP_Text _teamAMatchLabel;
        [SerializeField] private TMP_Text _teamBMatchLabel;

        [Header("Actions")]
        [SerializeField] private Button _continueButton;

        // ------------------------------------------------------------------

        private void Awake()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Populates all labels, shows the panel, and invokes
        /// <paramref name="onContinue"/> when the Continue button is clicked.
        /// </summary>
        public void Show(RoundEndData data, Action onContinue)
        {
            var r = data.Result;

            // ---- contract info -------------------------------------------
            SetText(_contractTeamLabel,  $"Contract:  {r.BiddingTeam}");
            SetText(_contractPlayerLabel, $"Player:    {data.BidderName}");

            string trumpStr = data.IsKaput
                ? $"Kaput {data.Trump}  (Bid {data.ContractBidValue})"
                : data.Trump == Suit.NoTrump
                    ? $"NoTrump  (Bid {data.ContractBidValue})"
                    : $"{data.Trump}  (Bid {data.ContractBidValue})";
            SetText(_trumpLabel, trumpStr);

            SetText(_bidValueLabel, $"Bid: {data.ContractBidValue}   Target: {data.ContractTargetPoints}");

            // ---- contract status -----------------------------------------
            if (_contractStatusLabel != null)
            {
                _contractStatusLabel.text  = r.ContractMet ? "✓  CONTRACT MET" : "✗  CONTRACT FAILED";
                _contractStatusLabel.color = r.ContractMet
                    ? new Color(0.2f, 0.8f, 0.2f)
                    : new Color(0.9f, 0.2f, 0.2f);
            }

            // ---- round scores --------------------------------------------
            SetText(_teamAScoreLabel, $"TeamA:  +{r.TeamAPoints} pts this round");
            SetText(_teamBScoreLabel, $"TeamB:  +{r.TeamBPoints} pts this round");

            if (_teamADeclLabel != null)
            {
                _teamADeclLabel.gameObject.SetActive(data.TeamADeclarationPts > 0);
                SetText(_teamADeclLabel, $"  Declarations: +{data.TeamADeclarationPts} raw");
            }
            if (_teamBDeclLabel != null)
            {
                _teamBDeclLabel.gameObject.SetActive(data.TeamBDeclarationPts > 0);
                SetText(_teamBDeclLabel, $"  Declarations: +{data.TeamBDeclarationPts} raw");
            }

            if (_challengeBonusLabel != null)
            {
                bool hasBonus = r.ChallengeBonus > 0;
                _challengeBonusLabel.gameObject.SetActive(hasBonus);
                if (hasBonus)
                    SetText(_challengeBonusLabel,
                        $"Challenge bonus:  +{r.ChallengeBonus} to {r.Winner}  (×{r.ChallengeBonusMultiplier})");
            }

            // ---- match totals --------------------------------------------
            SetText(_teamAMatchLabel, $"TeamA total:  {r.TeamAMatchTotal}");
            SetText(_teamBMatchLabel, $"TeamB total:  {r.TeamBMatchTotal}");

            // ---- continue button -----------------------------------------
            if (_continueButton != null)
            {
                _continueButton.onClick.RemoveAllListeners();
                _continueButton.onClick.AddListener(() =>
                {
                    Debug.Log("[Round Result] Continue clicked");
                    gameObject.SetActive(false);
                    onContinue?.Invoke();
                });
            }

            gameObject.SetActive(true);
            Debug.Log("[Round Result] Window opened");
        }

        private static void SetText(TMP_Text label, string text)
        {
            if (label != null) label.text = text;
        }
    }
}
