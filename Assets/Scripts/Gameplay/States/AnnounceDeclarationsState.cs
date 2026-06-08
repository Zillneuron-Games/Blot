using System.Collections.Generic;
using Blot.Core.StateMachine;
using Blot.Declarations;
using Blot.Gameplay.Events;
using Blot.Players;
using UnityEngine;

namespace Blot.Gameplay.States
{
    /// <summary>
    /// Collects declaration announcements from all four players before trick 1 starts.
    ///
    /// Turn order: same clockwise order as bidding (from Round Starter).
    /// Each player either announces their declarations or announces nothing.
    /// After all four have announced, the winning team is determined and the
    /// state transitions to PlayTrick.
    /// </summary>
    public class AnnounceDeclarationsState : IGameState
    {
        public GameStateId StateId => GameStateId.AnnounceDeclarations;

        private GameContext  _ctx;
        private List<Player> _order;
        private int          _index;

        public void Enter(GameContext ctx)
        {
            _ctx   = ctx;
            _index = 0;

            // Initialise (or reset) the manager for this round.
            ctx.DeclarationManager.Initialize(
                ctx.RoundManager.Trump,
                ctx.MatchManager.Players);

            // Announcement order: from Round Starter, clockwise.
            int startIdx = ctx.RoundManager.RoundStarterIndex;
            _order = new List<Player>(4);
            for (int i = 0; i < 4; i++)
                _order.Add(ctx.MatchManager.Players[(startIdx + i) % 4]);

            Debug.Log($"[AnnounceDeclarations] Starting — Trump = {ctx.RoundManager.Trump}");
            RequestNextAnnounce();
        }

        private void RequestNextAnnounce()
        {
            if (_index >= _order.Count)
            {
                FinalizeAnnouncements();
                return;
            }

            var player = _order[_index];
            player.OnDeclarationsChosen += HandleDeclarationsChosen;
            player.RequestDeclare(_ctx.RoundManager.Trump);
        }

        private void HandleDeclarationsChosen(List<Declaration> chosen)
        {
            var player = _order[_index];
            player.OnDeclarationsChosen -= HandleDeclarationsChosen;

            _ctx.DeclarationManager.Announce(player, chosen);
            GameEvents.DeclarationsAnnounced(player, chosen);

            _index++;
            RequestNextAnnounce();
        }

        private void FinalizeAnnouncements()
        {
            _ctx.DeclarationManager.FinalizeAnnouncements();
            GameEvents.DeclarationWinnerDetermined(_ctx.DeclarationManager.WinningTeam);

            Debug.Log($"[AnnounceDeclarations] Complete. " +
                      $"Winning team: {_ctx.DeclarationManager.WinningTeam?.ToString() ?? "none"}");

            _ctx.StateMachine.TransitionTo(GameStateId.PlayTrick);
        }

        public void Exit(GameContext ctx) { }
    }
}
