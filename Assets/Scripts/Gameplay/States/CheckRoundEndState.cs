using Blot.Core.StateMachine;
using Blot.Players;

namespace Blot.Gameplay.States
{
    public class CheckRoundEndState : IGameState
    {
        public GameStateId StateId => GameStateId.CheckRoundEnd;

        public void Enter(GameContext ctx)
        {
            int tricks = ctx.RoundManager.TricksPlayed;
            var dm     = ctx.DeclarationManager;

            // ---- After trick 1: winner-team reveal window ------------------
            // Only if a winner team exists and their reveal hasn't been processed.
            if (tricks == 1
                && dm.WinningTeam.HasValue
                && !dm.WinnerRevealCompleted)
            {
                ctx.StateMachine.TransitionTo(GameStateId.RevealDeclarations);
                return;
            }

            // ---- After trick 2: opponent reveal window (winner failed) -----
            // Only if the winner team failed to reveal AND the opponent has declarations.
            if (tricks == 2
                && dm.WinnerRevealCompleted
                && !dm.WinnerRevealSuccessful
                && !dm.OpponentRevealCompleted
                && dm.WinningTeam.HasValue)
            {
                TeamId opp = dm.WinningTeam.Value == TeamId.TeamA
                    ? TeamId.TeamB : TeamId.TeamA;

                if (dm.HasTeamDeclarations(opp))
                {
                    ctx.StateMachine.TransitionTo(GameStateId.RevealDeclarations);
                    return;
                }
            }

            // ---- Normal flow -----------------------------------------------
            ctx.StateMachine.TransitionTo(
                tricks >= 8 ? GameStateId.RoundEnd : GameStateId.PlayTrick);
        }

        public void Exit(GameContext ctx) { }
    }
}
