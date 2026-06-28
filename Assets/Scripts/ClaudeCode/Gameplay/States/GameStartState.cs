using Blot.Core.StateMachine;
using UnityEngine;

namespace Blot.Gameplay.States
{
    public class GameStartState : IGameState
    {
        public GameStateId StateId => GameStateId.GameStart;

        public void Enter(GameContext ctx)
        {
            Debug.Log("[Match Start] New match beginning.");

            // Randomly select the initial Round Starter (once per match).
            // Every subsequent round advances the starter clockwise in RoundEndState.
            int starterIndex = ctx.MatchManager.SelectRandomLeadPlayer();
            ctx.RoundManager.SetInitialRoundStarter(starterIndex);

            ctx.ScoreManager.ResetRoundScores();
            ctx.StateMachine.TransitionTo(GameStateId.DealCards);
        }

        public void Exit(GameContext ctx) { }
    }
}
