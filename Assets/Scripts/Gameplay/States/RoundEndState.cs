using Blot.Core.StateMachine;
using Blot.Gameplay.Events;
using Blot.Players;
using Blot.Scoring;
using Blot.UI;
using UnityEngine;

namespace Blot.Gameplay.States
{
    public class RoundEndState : IGameState
    {
        public GameStateId StateId => GameStateId.RoundEnd;

        public void Enter(GameContext ctx)
        {
            var rm = ctx.RoundManager;
            var dm = ctx.DeclarationManager;

            // ---- 1. Declaration bonuses ------------------------------------
            // Must be added BEFORE FinalizeRound so they are included in the
            // contract-success check and potential redistribution.
            int teamADeclPts = dm.GetValidDeclarationPoints(TeamId.TeamA);
            int teamBDeclPts = dm.GetValidDeclarationPoints(TeamId.TeamB);

            if (teamADeclPts > 0)
            {
                ctx.ScoreManager.AddTrickPoints(TeamId.TeamA, teamADeclPts);
                Debug.Log($"[Declarations] TeamA declaration bonus: +{teamADeclPts}");
            }
            if (teamBDeclPts > 0)
            {
                ctx.ScoreManager.AddTrickPoints(TeamId.TeamB, teamBDeclPts);
                Debug.Log($"[Declarations] TeamB declaration bonus: +{teamBDeclPts}");
            }

            // ---- 2. Undeclared Kaput upgrade --------------------------------
            // If a team wins all 8 tricks without having declared a Kaput contract,
            // their card value is upgraded from 162 to 250 for contract evaluation.
            // Declaration bonuses are added on top of the upgraded 250.
            if (!rm.IsKaputContract)
            {
                const int kaputUpgrade = 250 - 162;   // = 88

                if (rm.DidTeamWinAllTricks(TeamId.TeamA))
                {
                    ctx.ScoreManager.AddTrickPoints(TeamId.TeamA, kaputUpgrade);
                    Debug.Log($"[Kaput Achieved] TeamA won all tricks");
                    Debug.Log($"[Kaput Achieved] Card value upgraded from 162 to 250");
                }
                else if (rm.DidTeamWinAllTricks(TeamId.TeamB))
                {
                    ctx.ScoreManager.AddTrickPoints(TeamId.TeamB, kaputUpgrade);
                    Debug.Log($"[Kaput Achieved] TeamB won all tricks");
                    Debug.Log($"[Kaput Achieved] Card value upgraded from 162 to 250");
                }
            }

            // ---- 3. Contract finalisation ----------------------------------
            Debug.Log($"[Round {rm.CurrentRoundIndex} End] " +
                      $"Bidding team: {rm.BiddingTeam} | " +
                      (rm.IsKaputContract
                          ? $"Contract type: Kaput (win all tricks)"
                          : $"Target: {rm.ContractTargetPoints}"));

            // For declared Kaput contracts, determine success based on trick count,
            // not card points. Kaput succeeds only if the opponent won 0 tricks.
            bool? kaputContractMet = null;
            if (rm.IsKaputContract)
            {
                TeamId opponentTeam   = rm.BiddingTeam == TeamId.TeamA ? TeamId.TeamB : TeamId.TeamA;
                int    opponentTricks = rm.GetTricksWon(opponentTeam);
                bool   kaputSuccess   = opponentTricks == 0;
                kaputContractMet      = kaputSuccess;

                Debug.Log($"[Kaput Result] ContractTeam = {rm.BiddingTeam}, " +
                          $"OpponentTricks = {opponentTricks}, Success = {kaputSuccess}");
            }

            var result = ctx.ScoreManager.FinalizeRound(
                rm.BiddingTeam,
                rm.ContractBidValue,
                rm.ContractTargetPoints,
                kaputContractMet);

            if (result.ContractMet)
                Debug.Log($"[Contract Success] {rm.BiddingTeam} scored " +
                          $"{result.BidderRoundScore}" +
                          (rm.IsKaputContract ? " (Kaput)" : $" / Target {rm.ContractTargetPoints}"));
            else
                Debug.Log($"[Contract Failed] {rm.BiddingTeam} scored " +
                          $"{result.BidderRoundScore}" +
                          (rm.IsKaputContract ? " (Kaput failed)" : $" / Target {rm.ContractTargetPoints}"));

            // ---- 4. Challenge bonus ----------------------------------------
            // Applied AFTER FinalizeRound — goes directly to the round winner's
            // match total, bypassing the contract redistribution.
            int challengeBonus = 0;
            int multiplier     = rm.ChallengeBonusMultiplier;

            if (rm.IsChallengeActive && multiplier > 0)
            {
                challengeBonus = rm.ContractBidValue * multiplier;
                ctx.ScoreManager.AddMatchBonus(result.Winner, challengeBonus);

                Debug.Log($"[Challenge Bonus] {result.Winner} +{challengeBonus} " +
                          $"(ContractBid={rm.ContractBidValue} × {multiplier} | " +
                          $"I'm Sure={rm.IsSureConfirmed})");

                GameEvents.ChallengeBonus(result.Winner, challengeBonus);
            }

            // ---- 5. Build final result with updated match totals -----------
            var finalResult = new RoundResult(
                result.Winner, result.BiddingTeam, result.ContractMet,
                result.ContractBidValue, result.ContractTargetPoints, result.BidderRoundScore,
                result.TeamAPoints, result.TeamBPoints,
                ctx.ScoreManager.GetMatchScore(TeamId.TeamA),
                ctx.ScoreManager.GetMatchScore(TeamId.TeamB),
                challengeBonus, multiplier);

            GameEvents.RoundEnded(finalResult);

            // ---- 6. Show round result panel, then advance ----------------
            // AdvanceRoundStarter() is deferred until the human clicks Continue
            // so the RoundManager data remains valid while the panel is open.
            var endData = new RoundEndData(
                finalResult,
                teamADeclPts,
                teamBDeclPts,
                rm.BiddingPlayer?.Name ?? "Unknown",
                rm.Trump,
                rm.IsKaputContract,
                rm.ContractBidValue,
                rm.ContractTargetPoints);

            var pacing = GamePresentationController.Instance;
            if (pacing != null)
            {
                pacing.ShowRoundResult(endData, () => AdvanceToNextRound(ctx));
            }
            else
            {
                // No presentation controller in scene — advance immediately.
                AdvanceToNextRound(ctx);
            }
        }

        private static void AdvanceToNextRound(GameContext ctx)
        {
            var rm = ctx.RoundManager;
            rm.AdvanceRoundStarter();

            Debug.Log($"[Round End] " +
                      $"Next Round Starter = Player{rm.CurrentRoundStarter.Id} " +
                      $"({rm.CurrentRoundStarter.Name})");

            ctx.StateMachine.TransitionTo(GameStateId.CheckMatchEnd);
        }

        public void Exit(GameContext ctx) { }
    }
}
