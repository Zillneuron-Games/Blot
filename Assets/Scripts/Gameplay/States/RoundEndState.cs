using Blot.Core.StateMachine;
using Blot.Gameplay.Events;
using UnityEngine;

namespace Blot.Gameplay.States
{
    public class RoundEndState : IGameState
    {
        public GameStateId StateId => GameStateId.RoundEnd;

        public void Enter(GameContext ctx)
        {
            var rm = ctx.RoundManager;

            Debug.Log($"[Round {rm.CurrentRoundIndex} End] " +
                      $"Finalizing scores. Bidding team: {rm.BiddingTeam} | " +
                      $"Target: {rm.ContractTargetPoints}");

            var result = ctx.ScoreManager.FinalizeRound(
                rm.BiddingTeam,
                rm.ContractBidValue,
                rm.ContractTargetPoints);

            if (result.ContractMet)
                Debug.Log($"[Contract Success] {rm.BiddingTeam} scored " +
                          $"{result.BidderRoundScore} / Target {rm.ContractTargetPoints}");
            else
                Debug.Log($"[Contract Failed] {rm.BiddingTeam} scored " +
                          $"{result.BidderRoundScore} / Target {rm.ContractTargetPoints}");

            GameEvents.RoundEnded(result);

            ctx.RoundManager.AdvanceRoundStarter();

            Debug.Log($"[Round {rm.CurrentRoundIndex} End] " +
                      $"Next Round Starter = Player{rm.CurrentRoundStarter.Id} " +
                      $"({rm.CurrentRoundStarter.Name})");

            ctx.StateMachine.TransitionTo(GameStateId.CheckMatchEnd);
        }

        public void Exit(GameContext ctx) { }
    }
}
