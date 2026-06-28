using System;
using Blot.Core.StateMachine;
using Blot.Gameplay.Events;
using Blot.UI;
using UnityEngine;

namespace Blot.Gameplay.States
{
    public class EvaluateTrickState : IGameState
    {
        public GameStateId StateId => GameStateId.EvaluateTrick;

        public void Enter(GameContext ctx)
        {
            var trick  = ctx.RoundManager.CurrentTrick;
            var winner = trick.GetWinner();
            int points = trick.GetTotalPoints();

            ctx.ScoreManager.AddTrickPoints(winner.Team, points);
            ctx.RoundManager.CompleteTrick(winner);

            // Last-trick bonus: +10 to the team that wins trick 8
            if (ctx.RoundManager.TricksPlayed == 8)
                ctx.ScoreManager.AddTrickPoints(winner.Team, 10);

            GameEvents.TrickCompleted(trick);

            Debug.Log($"[Pacing] Showing trick result ({TrickResultDelay:F2}s before advancing)");

            // Hold here so all 4 cards stay visible and the winner message is readable.
            Advance(TrickResultDelay, () => ctx.StateMachine.TransitionTo(GameStateId.CheckRoundEnd));
        }

        public void Exit(GameContext ctx) { }

        // ------------------------------------------------------------------ pacing helpers

        private static float TrickResultDelay =>
            GamePresentationController.Instance?.TrickResultDelay ?? 0f;

        private static void Advance(float delay, Action action)
        {
            var pacing = GamePresentationController.Instance;
            if (pacing != null && delay > 0f)
                pacing.RunAfterDelay(delay, action);
            else
                action.Invoke();
        }
    }
}
