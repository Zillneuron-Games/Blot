using System;
using System.Collections.Generic;
using Blot.Cards;
using Blot.Gameplay;
using Blot.Gameplay.Rules;
using Blot.Players;
using UnityEngine;

namespace Blot.Core.Managers
{
    /// <summary>
    /// Single source of truth for all turn-order state within a match.
    ///
    /// Turn-order properties (read by any system that needs them):
    ///   CurrentRoundStarter   – player who leads trick 1 of each round
    ///   CurrentTrickLeader    – player who leads the current trick
    ///   CurrentActivePlayer   – player currently being asked to play a card
    ///   CurrentTrickWinner    – winner of the most recently completed trick
    ///   CurrentTrickIndex     – 1-based trick index within the round (1–8)
    ///   CurrentRoundIndex     – 1-based round index within the match
    ///   CardsPlayedThisTrick  – cards committed in the current trick (0–4)
    ///
    /// Clockwise seat order: Player0 → Player1 → Player2 → Player3 → Player0
    /// </summary>
    public class RoundManager
    {
        // ==================================================================
        // Turn-order state — single source of truth
        // ==================================================================

        /// <summary>Player who leads the first trick of the current round.</summary>
        public Player CurrentRoundStarter  { get; private set; }

        /// <summary>Seat index of the current Round Starter.</summary>
        public int    RoundStarterIndex    { get; private set; }

        /// <summary>Player who leads the current trick (= player at LeadPlayerIndex).</summary>
        public Player CurrentTrickLeader   => _players[LeadPlayerIndex];

        /// <summary>Player currently being asked to play a card.</summary>
        public Player CurrentActivePlayer  { get; private set; }

        /// <summary>Winner of the most recently completed trick.</summary>
        public Player CurrentTrickWinner   { get; private set; }

        /// <summary>1-based trick index within the current round (1 = first trick).</summary>
        public int    CurrentTrickIndex    { get; private set; }

        /// <summary>1-based round index within the current match (1 = first round).</summary>
        public int    CurrentRoundIndex    { get; private set; }

        /// <summary>Number of cards committed in the current trick (0–4).</summary>
        public int    CardsPlayedThisTrick { get; private set; }

        // ==================================================================
        // Round state
        // ==================================================================

        public Suit  Trump           { get; private set; }
        public Trick CurrentTrick    { get; private set; }
        public int   TricksPlayed    { get; private set; }   // completed tricks this round (0–8)
        public int   LeadPlayerIndex { get; private set; }   // seat index of current trick leader

        /// <summary>All four players in seat order (index 0–3).</summary>
        public IReadOnlyList<Player> Players => _players;

        /// <summary>The player who won the bidding and chose trump.</summary>
        public Player BiddingPlayer { get; private set; }

        /// <summary>Team of the bidder; defaults to TeamA if not set.</summary>
        public TeamId BiddingTeam => BiddingPlayer?.Team ?? TeamId.TeamA;

        /// <summary>
        /// Effective bid value (e.g. 10 for normal, 25+ for Kaput).
        /// Used for challenge-bonus calculation and scoring award.
        /// </summary>
        public int ContractBidValue     { get; private set; }

        /// <summary>
        /// ContractBidValue × 10. Added to the contract team's raw award on success,
        /// or to the defender's raw award on failure.
        /// For Kaput contracts, the win condition is trick-based, not points-based.
        /// </summary>
        public int ContractTargetPoints { get; private set; }

        // ---- challenge state ("I Don't Believe" / "I'm Sure") --------------
        /// <summary>True if an opponent challenged the contract during bidding.</summary>
        public bool   IsChallengeActive       { get; private set; }
        /// <summary>The player who issued the challenge.</summary>
        public Player ChallengePlayer         { get; private set; }
        /// <summary>Team of the challenger.</summary>
        public TeamId ChallengeTeam           { get; private set; }
        /// <summary>True if the contract team responded "I'm Sure".</summary>
        public bool   IsSureConfirmed         { get; private set; }
        /// <summary>
        /// Multiplier for the challenge bonus:
        ///   0 = no challenge active
        ///   1 = challenged only ("I Don't Believe")
        ///   3 = challenged + confirmed ("I Don't Believe" + "I'm Sure")
        /// Applied to ContractBidValue → extra match points for the round winner.
        /// </summary>
        public int    ChallengeBonusMultiplier { get; private set; }

        /// <summary>Winner of trick 8 (used for the last-trick +10 bonus).</summary>
        public Player LastTrickWinner { get; private set; }

        /// <summary>Tracks Belote / Rebelote state for the current round.</summary>
        public BeloteTracker BeloteTracker { get; private set; }

        // ---- Kaput contract state -----------------------------------------
        /// <summary>True if the current contract is a declared Kaput contract.</summary>
        public bool IsKaputContract  { get; private set; }
        /// <summary>Additional bonus value declared on top of the base Kaput value of 25.</summary>
        public int  KaputExtraValue  { get; private set; }

        // ---- Per-team trick tracking --------------------------------------
        private readonly int[] _tricksWonByTeam = new int[2];

        /// <summary>Returns the number of tricks won by the specified team this round.</summary>
        public int GetTricksWon(TeamId team) => _tricksWonByTeam[(int)team];

        /// <summary>Returns true if the specified team won all 8 tricks this round.</summary>
        public bool DidTeamWinAllTricks(TeamId team) => _tricksWonByTeam[(int)team] == 8;

        // ==================================================================
        // Private
        // ==================================================================

        private readonly Player[] _players;

        public RoundManager(Player[] players) => _players = players;

        // ==================================================================
        // Helper methods
        // ==================================================================

        /// <summary>Returns the player immediately clockwise of <paramref name="player"/>.</summary>
        public Player GetNextClockwisePlayer(Player player)
        {
            int idx = Array.IndexOf(_players, player);
            return _players[(idx + 1) % _players.Length];
        }

        /// <summary>Returns the player at <paramref name="index"/> (wraps around).</summary>
        public Player GetPlayerByIndex(int index) =>
            _players[((index % _players.Length) + _players.Length) % _players.Length];

        /// <summary>
        /// Advances <see cref="CurrentActivePlayer"/> one step clockwise.
        /// Can be used by any system that needs to manually step through players.
        /// </summary>
        public void AdvanceToNextPlayer()
        {
            if (CurrentActivePlayer != null)
                CurrentActivePlayer = GetNextClockwisePlayer(CurrentActivePlayer);
        }

        // ==================================================================
        // Match initialisation
        // ==================================================================

        /// <summary>
        /// Call once when a new match starts.
        /// Randomly selects the initial Round Starter and resets match-level counters.
        /// </summary>
        public void SetInitialRoundStarter(int starterIndex)
        {
            RoundStarterIndex   = starterIndex;
            CurrentRoundStarter = _players[starterIndex];
            CurrentRoundIndex   = 0;   // incremented to 1 inside StartRound

            Debug.Log($"[Match Start] Initial Round Starter = " +
                      $"Player{CurrentRoundStarter.Id} ({CurrentRoundStarter.Name})");
        }

        /// <summary>
        /// Advances the Round Starter one step clockwise.
        /// Call at the end of each completed round, before starting the next one.
        /// </summary>
        public void AdvanceRoundStarter()
        {
            var previous       = CurrentRoundStarter;
            RoundStarterIndex  = (RoundStarterIndex + 1) % _players.Length;
            CurrentRoundStarter = _players[RoundStarterIndex];

            Debug.Log($"[Round Starter] {previous.Name} → {CurrentRoundStarter.Name}");
        }

        // ==================================================================
        // Round control
        // ==================================================================

        /// <summary>
        /// Starts a new round. The Round Starter (set via
        /// <see cref="SetInitialRoundStarter"/> or <see cref="AdvanceRoundStarter"/>)
        /// automatically leads trick 1.
        /// </summary>
        /// <param name="trump">Trump suit chosen during bidding.</param>
        /// <param name="bidder">Player who won the bid (may be null).</param>
        /// <param name="bidValue">Effective bid value (EffectiveBidValue of the winning Bid).</param>
        /// <param name="isKaput">True if the contract is a declared Kaput.</param>
        /// <param name="kaputExtra">KaputExtraValue of the bid (0 if not Kaput).</param>
        public void StartRound(Suit trump, Player bidder = null, int bidValue = 0,
                               bool isKaput = false, int kaputExtra = 0)
        {
            Trump                  = trump;
            TricksPlayed           = 0;
            CurrentTrickIndex      = 0;
            LeadPlayerIndex        = RoundStarterIndex;
            BiddingPlayer          = bidder;
            ContractBidValue       = bidValue;
            ContractTargetPoints   = bidValue * 10;   // used for scoring award; win condition may be overridden for Kaput
            IsKaputContract        = isKaput;
            KaputExtraValue        = kaputExtra;
            _tricksWonByTeam[0]    = 0;
            _tricksWonByTeam[1]    = 0;
            LastTrickWinner        = null;
            CurrentTrickWinner     = null;
            CurrentActivePlayer    = null;
            // Reset challenge state (SetChallengeState is called AFTER this if needed).
            IsChallengeActive        = false;
            ChallengePlayer          = null;
            IsSureConfirmed          = false;
            ChallengeBonusMultiplier = 0;
            CurrentRoundIndex++;
            BeloteTracker          = new BeloteTracker(trump, _players);

            Debug.Log($"[Round {CurrentRoundIndex} Start] " +
                      $"Starter = Player{CurrentRoundStarter.Id} ({CurrentRoundStarter.Name}) | " +
                      $"Trump = {trump} | " +
                      $"Bidder = {bidder?.Name ?? "none"} | " +
                      $"Target = {ContractTargetPoints}");

            BeginNewTrick();
        }

        // ==================================================================
        // Trick control
        // ==================================================================

        /// <summary>Prepares a fresh Trick object for the next trick in the round.</summary>
        public void BeginNewTrick()
        {
            CurrentTrickIndex++;
            CardsPlayedThisTrick = 0;
            CurrentTrick         = new Trick(Trump);
        }

        /// <summary>Returns the four players in the order they act this trick,
        /// starting clockwise from <see cref="CurrentTrickLeader"/>.</summary>
        public List<Player> GetTrickTurnOrder()
        {
            var order = new List<Player>(4);
            for (int i = 0; i < 4; i++)
                order.Add(_players[(LeadPlayerIndex + i) % _players.Length]);
            return order;
        }

        /// <summary>
        /// Updates <see cref="CurrentActivePlayer"/>.
        /// Call from PlayTrickState whenever a new player's turn begins.
        /// </summary>
        public void SetActivePlayer(Player player)
        {
            CurrentActivePlayer = player;
        }

        /// <summary>
        /// Increments <see cref="CardsPlayedThisTrick"/>.
        /// Call from PlayTrickState immediately after a card is committed.
        /// </summary>
        public void NotifyCardPlayed()
        {
            CardsPlayedThisTrick++;
        }

        /// <summary>
        /// Records the trick as complete: stores the winner, updates the lead player,
        /// and (when the round has more tricks remaining) starts the next trick.
        /// </summary>
        public void CompleteTrick(Player winner)
        {
            CurrentTrickWinner = winner;
            TricksPlayed++;
            LeadPlayerIndex    = Array.IndexOf(_players, winner);
            _tricksWonByTeam[(int)winner.Team]++;

            Debug.Log($"[Trick {CurrentTrickIndex} End] " +
                      $"Winner = Player{winner.Id} ({winner.Name}) | " +
                      $"Tricks completed this round: {TricksPlayed}/8");

            if (TricksPlayed == 8)
                LastTrickWinner = winner;

            if (TricksPlayed < 8)
                BeginNewTrick();
        }

        // ==================================================================
        // StartRound / EndTrick / EndRound aliases for naming clarity
        // ==================================================================

        // ==================================================================
        // Challenge state
        // ==================================================================

        /// <summary>
        /// Records that a challenge occurred during this round's bidding.
        /// Must be called AFTER <see cref="StartRound"/> (which resets challenge state).
        /// </summary>
        public void SetChallengeState(Player challenger, bool isSureConfirmed)
        {
            IsChallengeActive        = true;
            ChallengePlayer          = challenger;
            ChallengeTeam            = challenger.Team;
            IsSureConfirmed          = isSureConfirmed;
            ChallengeBonusMultiplier = isSureConfirmed ? 3 : 1;

            Debug.Log($"[Challenge State] Challenger = Player{challenger.Id} ({challenger.Name}) | " +
                      $"I'm Sure = {isSureConfirmed} | Multiplier = {ChallengeBonusMultiplier}");
        }

        /// <summary>Alias: starts a round (identical to <see cref="StartRound"/>).</summary>
        public void StartTrick(Player leader)
        {
            LeadPlayerIndex      = Array.IndexOf(_players, leader);
            CardsPlayedThisTrick = 0;
            CurrentTrick         = new Trick(Trump);
        }
    }
}
