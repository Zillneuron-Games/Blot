using Blot.Core.StateMachine;

namespace Blot.Gameplay.States
{
    public class CheckRoundEndState : IGameState
    {
        public GameStateId StateId => GameStateId.CheckRoundEnd;

        public void Enter(GameContext ctx)
        {
            var next = ctx.RoundManager.TricksPlayed >= 8
                ? GameStateId.RoundEnd
                : GameStateId.PlayTrick;

            ctx.StateMachine.TransitionTo(next);
        }

        public void Exit(GameContext ctx) { }
    }
}
