using Blot.Core.StateMachine;
using Blot.Gameplay.Events;

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
            ctx.StateMachine.TransitionTo(GameStateId.CheckRoundEnd);
        }

        public void Exit(GameContext ctx) { }
    }
}
