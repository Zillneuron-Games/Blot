using System.Collections.Generic;
using Blot.Cards;
using Blot.Players;
using UnityEngine;

namespace Blot.Declarations
{
    /// <summary>
    /// Single source of truth for the declaration (announcement + reveal) lifecycle.
    ///
    /// Lifecycle per round:
    ///   1. Initialize(trump, players)            — called by AnnounceDeclarationsState
    ///   2. Announce(player, declarations) × 4    — one per player
    ///   3. FinalizeAnnouncements()               — determines WinningTeam
    ///   4. [Trick 1 ends] RevealDeclarationsState runs for WinningTeam:
    ///        MarkRevealed(player) per winner-team player who confirms
    ///        FinalizeWinnerReveal(allRevealed)
    ///   5. [If winner failed — trick 2 ends] RevealDeclarationsState runs for opponent:
    ///        MarkRevealed(player) per opponent-team player who confirms
    ///        FinalizeOpponentReveal(allRevealed)
    ///   6. GetValidDeclarationPoints(team) — called by RoundEndState for scoring
    ///
    /// Belote/Rebelote is handled separately by BeloteTracker and is never affected
    /// by this manager.
    /// </summary>
    public class DeclarationManager
    {
        // ==================================================================
        // State
        // ==================================================================

        private Suit     _trump;
        private Player[] _players;

        // ---- announcement phase --------------------------------------------
        private readonly Dictionary<int, List<Declaration>> _announced = new();

        public bool AnnouncementComplete { get; private set; }

        /// <summary>
        /// Team holding the strongest single declaration, or null if no one declared.
        /// </summary>
        public TeamId? WinningTeam { get; private set; }

        // ---- reveal phase --------------------------------------------------
        private readonly HashSet<int> _revealedPlayerIds = new();

        public bool WinnerRevealCompleted   { get; private set; }
        public bool WinnerRevealSuccessful  { get; private set; }
        public bool OpponentRevealCompleted { get; private set; }

        // ---- scoring -------------------------------------------------------
        private TeamId? _validTeam;   // team whose declarations are actually worth points

        // ==================================================================
        // Initialise (once per round)
        // ==================================================================

        public void Initialize(Suit trump, Player[] players)
        {
            _trump   = trump;
            _players = players;

            _announced.Clear();
            _revealedPlayerIds.Clear();

            AnnouncementComplete   = false;
            WinningTeam            = null;
            WinnerRevealCompleted  = false;
            WinnerRevealSuccessful = false;
            OpponentRevealCompleted = false;
            _validTeam             = null;
        }

        // ==================================================================
        // Announcement phase
        // ==================================================================

        /// <summary>Records the declarations a player chose to announce. Empty list = pass.</summary>
        public void Announce(Player player, List<Declaration> declarations)
        {
            _announced[player.Id] = declarations ?? new List<Declaration>();
            Debug.Log($"[Declaration] Player{player.Id} ({player.Name}) announces " +
                      $"{declarations?.Count ?? 0} declaration(s).");
        }

        /// <summary>
        /// Call after all four players have announced.
        /// Determines which team holds the strongest declaration.
        /// </summary>
        public void FinalizeAnnouncements()
        {
            AnnouncementComplete = true;
            DetermineWinningTeam();
        }

        private void DetermineWinningTeam()
        {
            Declaration strongest    = null;
            TeamId      strongTeam   = TeamId.TeamA;

            foreach (var kvp in _announced)
            {
                var player = FindPlayer(kvp.Key);
                if (player == null) continue;

                foreach (var decl in kvp.Value)
                {
                    if (strongest == null ||
                        decl.GetStrengthRank(_trump) < strongest.GetStrengthRank(_trump))
                    {
                        strongest  = decl;
                        strongTeam = player.Team;
                    }
                }
            }

            WinningTeam = strongest != null ? strongTeam : (TeamId?)null;

            if (WinningTeam.HasValue)
                Debug.Log($"[Declaration] Winning team: {WinningTeam} (strongest: {strongest})");
            else
                Debug.Log("[Declaration] No declarations announced.");
        }

        // ==================================================================
        // Queries used by CheckRoundEndState
        // ==================================================================

        /// <summary>True if the team has at least one player with declared declarations.</summary>
        public bool HasTeamDeclarations(TeamId team)
        {
            foreach (var kvp in _announced)
            {
                var p = FindPlayer(kvp.Key);
                if (p != null && p.Team == team && kvp.Value.Count > 0) return true;
            }
            return false;
        }

        /// <summary>True if the player has at least one announced declaration.</summary>
        public bool HasPlayerDeclarations(Player player) =>
            _announced.TryGetValue(player.Id, out var d) && d.Count > 0;

        /// <summary>Returns the player's announced declarations (empty list if none).</summary>
        public List<Declaration> GetAnnouncedDeclarations(Player player) =>
            _announced.TryGetValue(player.Id, out var d) ? d : new List<Declaration>();

        // ==================================================================
        // Reveal phase
        // ==================================================================

        /// <summary>Records that <paramref name="player"/> has confirmed their reveal.</summary>
        public void MarkRevealed(Player player)
        {
            _revealedPlayerIds.Add(player.Id);
            Debug.Log($"[Declaration] Player{player.Id} ({player.Name}) revealed declarations.");
        }

        /// <summary>
        /// Returns true when every player on <paramref name="team"/> who has declarations
        /// has confirmed their reveal.
        /// </summary>
        public bool DidAllTeamPlayersReveal(TeamId team)
        {
            foreach (var kvp in _announced)
            {
                if (kvp.Value.Count == 0) continue;
                var p = FindPlayer(kvp.Key);
                if (p != null && p.Team == team && !_revealedPlayerIds.Contains(p.Id))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Call after the winning-team reveal window closes.
        /// <paramref name="success"/> = all winning-team players with declarations confirmed.
        /// </summary>
        public void FinalizeWinnerReveal(bool success)
        {
            WinnerRevealCompleted  = true;
            WinnerRevealSuccessful = success;

            if (success)
            {
                _validTeam = WinningTeam;
                Debug.Log($"[Declaration] Winner reveal SUCCESS — {WinningTeam} declarations valid.");
            }
            else
            {
                Debug.Log("[Declaration] Winner reveal FAILED — declarations voided.");
            }
        }

        /// <summary>
        /// Call after the opponent reveal window closes (trick 3 path).
        /// <paramref name="success"/> = all opponent players with declarations confirmed.
        /// </summary>
        public void FinalizeOpponentReveal(bool success)
        {
            OpponentRevealCompleted = true;

            if (success && WinningTeam.HasValue)
            {
                // WinningTeam is set when a declaration exists; .Value is safe here.
                _validTeam = WinningTeam.Value == TeamId.TeamA ? TeamId.TeamB : TeamId.TeamA;
                Debug.Log($"[Declaration] Opponent reveal SUCCESS — {_validTeam} declarations valid.");
            }
            else
            {
                Debug.Log("[Declaration] Opponent reveal FAILED — no declaration bonuses awarded.");
            }
        }

        // ==================================================================
        // Scoring
        // ==================================================================

        /// <summary>
        /// Total declaration points for <paramref name="team"/> that were successfully revealed.
        /// Returns 0 if this team's declarations were not validated.
        /// Call this from RoundEndState before FinalizeRound.
        /// </summary>
        public int GetValidDeclarationPoints(TeamId team)
        {
            if (_validTeam != team) return 0;

            int total = 0;
            foreach (var kvp in _announced)
            {
                var p = FindPlayer(kvp.Key);
                if (p != null && p.Team == team)
                    foreach (var decl in kvp.Value)
                        total += decl.GetValue(_trump);
            }
            return total;
        }

        // ==================================================================
        // Helper
        // ==================================================================

        private Player FindPlayer(int id)
        {
            if (_players == null) return null;
            foreach (var p in _players) if (p.Id == id) return p;
            return null;
        }
    }
}
