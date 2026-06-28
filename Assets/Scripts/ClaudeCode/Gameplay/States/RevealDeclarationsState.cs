using System.Collections.Generic;
using Blot.Core.StateMachine;
using Blot.Gameplay.Events;
using Blot.Players;
using UnityEngine;

namespace Blot.Gameplay.States
{
    /// <summary>
    /// Handles the declaration reveal window.
    ///
    /// Entered from CheckRoundEndState in two possible situations:
    ///   • After trick 1 (TricksPlayed == 1):
    ///       The winning team must reveal their declarations.
    ///   • After trick 2 (TricksPlayed == 2, winner failed):
    ///       The opposing team may reveal their declarations.
    ///
    /// After all relevant players have been processed the state transitions
    /// back to PlayTrick.
    /// </summary>
    public class RevealDeclarationsState : IGameState
    {
        public GameStateId StateId => GameStateId.RevealDeclarations;

        private GameContext  _ctx;
        private List<Player> _revealPlayers;   // players who need to reveal this window
        private int          _revealIndex;
        private TeamId       _teamToReveal;
        private bool         _isWinnerReveal;

        public void Enter(GameContext ctx)
        {
            _ctx         = ctx;
            _revealIndex = 0;

            var  dm     = ctx.DeclarationManager;
            int  tricks = ctx.RoundManager.TricksPlayed;

            _isWinnerReveal = tricks == 1;
            // WinningTeam.HasValue is guaranteed by CheckRoundEndState's guards.
            _teamToReveal   = _isWinnerReveal
                ? dm.WinningTeam.Value
                : OtherTeam(dm.WinningTeam.Value);

            Debug.Log($"[RevealDeclarations] {(_isWinnerReveal ? "Winner" : "Opponent")} " +
                      $"reveal — team {_teamToReveal}, after trick {tricks}");

            // Build ordered list of team players who have declarations.
            _revealPlayers = new List<Player>();
            foreach (var p in ctx.MatchManager.Players)
            {
                if (p.Team == _teamToReveal && dm.HasPlayerDeclarations(p))
                    _revealPlayers.Add(p);
            }

            if (_revealPlayers.Count == 0)
            {
                // Nothing to reveal — close the window immediately.
                FinishReveal();
                return;
            }

            RequestNextReveal();
        }

        private void RequestNextReveal()
        {
            if (_revealIndex >= _revealPlayers.Count)
            {
                FinishReveal();
                return;
            }

            var player    = _revealPlayers[_revealIndex];
            var announced = _ctx.DeclarationManager.GetAnnouncedDeclarations(player);

            player.OnRevealResult += HandleRevealResult;
            player.RequestReveal(announced);
        }

        private void HandleRevealResult(bool confirmed)
        {
            var player = _revealPlayers[_revealIndex];
            player.OnRevealResult -= HandleRevealResult;

            if (confirmed)
            {
                _ctx.DeclarationManager.MarkRevealed(player);
                var announced = _ctx.DeclarationManager.GetAnnouncedDeclarations(player);
                GameEvents.DeclarationsRevealed(player, announced);
            }
            else
            {
                Debug.Log($"[RevealDeclarations] Player{player.Id} ({player.Name}) skipped reveal.");
            }

            _revealIndex++;
            RequestNextReveal();
        }

        private void FinishReveal()
        {
            var dm      = _ctx.DeclarationManager;
            bool allOk  = dm.DidAllTeamPlayersReveal(_teamToReveal);

            if (_isWinnerReveal)
                dm.FinalizeWinnerReveal(allOk);
            else
                dm.FinalizeOpponentReveal(allOk);

            _ctx.StateMachine.TransitionTo(GameStateId.PlayTrick);
        }

        private static TeamId OtherTeam(TeamId t) =>
            t == TeamId.TeamA ? TeamId.TeamB : TeamId.TeamA;

        public void Exit(GameContext ctx) { }
    }
}
