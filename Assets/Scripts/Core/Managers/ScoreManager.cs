using Blot.Players;
using Blot.Scoring;
using UnityEngine;

namespace Blot.Core.Managers
{
    public class ScoreManager
    {
        public const int WinTarget = 301;

        private readonly int[] _roundScores = new int[2];  // raw trick pts + bonuses this round
        private readonly int[] _matchScores = new int[2];  // cumulative score units

        // ------------------------------------------------------------------ round scoring

        public void ResetRoundScores()
        {
            _roundScores[0] = 0;
            _roundScores[1] = 0;
        }

        /// <summary>Add card-trick points (or bonus points) to a team's raw round total.</summary>
        public void AddTrickPoints(TeamId team, int points) =>
            _roundScores[(int)team] += points;

        // ------------------------------------------------------------------ conversion helper

        /// <summary>
        /// Converts raw card-point totals to score units.
        /// Divides by 10 and rounds up when the remainder is 5 or more.
        ///
        /// Examples: 90→9, 102→10, 85→9, 86→9, 88→9, 107→11
        /// </summary>
        public static int ConvertRawPointsToScoreUnits(int rawPoints)
        {
            int units     = rawPoints / 10;
            int remainder = rawPoints % 10;
            return remainder >= 5 ? units + 1 : units;
        }

        // ------------------------------------------------------------------ round finalisation

        /// <summary>
        /// Applies the contract rule, converts raw points to score units, and adds
        /// the results to cumulative match totals.
        ///
        /// Validation (raw, not converted):
        ///   contractMet = bidderRawScore >= contractTargetPoints
        ///   (For Kaput contracts pass <paramref name="forceContractMet"/> to use the
        ///   trick-based result determined by RoundEndState instead.)
        ///
        /// Success award:
        ///   Contract team raw = bidderRawScore + contractTargetPoints → convert to units
        ///   Opponent team raw = defenderRawScore → convert to units
        ///
        /// Failure award:
        ///   Contract team = 0
        ///   Opponent team raw = (allRoundPoints + contractTargetPoints) → convert to units
        ///
        /// Belote/Rebelote and declaration bonuses must be added via
        /// <see cref="AddTrickPoints"/> BEFORE calling this method.
        /// </summary>
        public RoundResult FinalizeRound(TeamId biddingTeam, int contractBidValue, int contractTargetPoints,
                                         bool? forceContractMet = null)
        {
            TeamId defenderTeam     = biddingTeam == TeamId.TeamA ? TeamId.TeamB : TeamId.TeamA;
            int    bidderRawScore   = _roundScores[(int)biddingTeam];
            int    defenderRawScore = _roundScores[(int)defenderTeam];

            bool contractMet = forceContractMet.HasValue
                ? forceContractMet.Value
                : bidderRawScore >= contractTargetPoints;

            Debug.Log($"[Scoring] ContractBid = {contractBidValue}, Target = {contractTargetPoints}");
            Debug.Log($"[Scoring] ContractTeam raw score = {bidderRawScore}");
            Debug.Log($"[Scoring] Contract success = {contractMet}");

            TeamId roundWinner;
            int    aRoundUnits, bRoundUnits;

            if (contractMet)
            {
                int bidderAwardRaw   = bidderRawScore + contractTargetPoints;
                int bidderAwardUnits = ConvertRawPointsToScoreUnits(bidderAwardRaw);
                int defenderAwardUnits = ConvertRawPointsToScoreUnits(defenderRawScore);

                _matchScores[(int)biddingTeam]  += bidderAwardUnits;
                _matchScores[(int)defenderTeam] += defenderAwardUnits;

                roundWinner = biddingTeam;
                aRoundUnits = biddingTeam == TeamId.TeamA ? bidderAwardUnits : defenderAwardUnits;
                bRoundUnits = biddingTeam == TeamId.TeamB ? bidderAwardUnits : defenderAwardUnits;

                Debug.Log($"[Scoring] Raw award = {bidderAwardRaw} " +
                          $"({bidderRawScore} cards + {contractTargetPoints} contract)");
                Debug.Log($"[Scoring] Final score units = {bidderAwardUnits}");
                Debug.Log($"[Scoring] Opponent score units = {defenderAwardUnits}");
            }
            else
            {
                int totalRaw           = _roundScores[0] + _roundScores[1] + contractTargetPoints;
                int defenderAwardUnits = ConvertRawPointsToScoreUnits(totalRaw);

                _matchScores[(int)defenderTeam] += defenderAwardUnits;

                roundWinner = defenderTeam;
                aRoundUnits = defenderTeam == TeamId.TeamA ? defenderAwardUnits : 0;
                bRoundUnits = defenderTeam == TeamId.TeamB ? defenderAwardUnits : 0;

                Debug.Log($"[Scoring] Failed — Opponent raw = {totalRaw} " +
                          $"(all cards + {contractTargetPoints} contract)");
                Debug.Log($"[Scoring] Opponent score units = {defenderAwardUnits}");
            }

            return new RoundResult(
                roundWinner, biddingTeam, contractMet,
                contractBidValue, contractTargetPoints, bidderRawScore,
                aRoundUnits, bRoundUnits,
                _matchScores[(int)TeamId.TeamA],
                _matchScores[(int)TeamId.TeamB]);
        }

        // ------------------------------------------------------------------ match bonus (challenge)

        /// <summary>
        /// Adds score units directly to a team's cumulative match total, bypassing the
        /// round-score contract logic.  Used for "I Don't Believe" / "I'm Sure" bonuses.
        /// Must be called AFTER <see cref="FinalizeRound"/>.
        /// </summary>
        public void AddMatchBonus(TeamId team, int scoreUnits) =>
            _matchScores[(int)team] += scoreUnits;

        // ------------------------------------------------------------------ match queries

        public int    GetMatchScore(TeamId team) => _matchScores[(int)team];
        public bool   IsMatchOver()              => _matchScores[0] >= WinTarget || _matchScores[1] >= WinTarget;
        public TeamId GetMatchWinner()           => _matchScores[(int)TeamId.TeamA] >= WinTarget
                                                    ? TeamId.TeamA : TeamId.TeamB;
    }
}
