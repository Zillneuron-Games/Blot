using Blot.Players;
using Blot.Scoring;

namespace Blot.Core.Managers
{
    public class ScoreManager
    {
        public const int WinTarget = 301;

        private readonly int[] _roundScores = new int[2];  // trick pts + bonuses this round
        private readonly int[] _matchScores = new int[2];  // cumulative match totals

        // ------------------------------------------------------------------ round scoring

        public void ResetRoundScores()
        {
            _roundScores[0] = 0;
            _roundScores[1] = 0;
        }

        /// <summary>Add card-trick points (or bonus points) to a team's round total.</summary>
        public void AddTrickPoints(TeamId team, int points) =>
            _roundScores[(int)team] += points;

        // ------------------------------------------------------------------ round finalisation

        /// <summary>
        /// Applies the contract rule and moves round scores into match totals.
        ///
        /// Contract rule:
        ///   • If <paramref name="biddingTeam"/> scored MORE points than the defending team
        ///     → both teams add their earned round points to their match totals.
        ///   • If bidding team FAILED (scored ≤ defending team)
        ///     → defending team receives ALL round points; bidding team gets 0.
        ///
        /// Belote/Rebelote bonuses and last-trick bonus must be added via
        /// <see cref="AddTrickPoints"/> BEFORE calling this method.
        /// </summary>
        public RoundResult FinalizeRound(TeamId biddingTeam)
        {
            int aRound = _roundScores[(int)TeamId.TeamA];
            int bRound = _roundScores[(int)TeamId.TeamB];

            TeamId defenderTeam  = biddingTeam == TeamId.TeamA ? TeamId.TeamB : TeamId.TeamA;
            int    bidderScore   = _roundScores[(int)biddingTeam];
            int    defenderScore = _roundScores[(int)defenderTeam];

            bool contractMet = bidderScore > defenderScore;
            TeamId roundWinner;

            if (contractMet)
            {
                // Both teams earn what they scored.
                _matchScores[(int)TeamId.TeamA] += aRound;
                _matchScores[(int)TeamId.TeamB] += bRound;
                roundWinner = biddingTeam;
            }
            else
            {
                // Defending team takes everything; bidder gets nothing.
                int total = aRound + bRound;
                _matchScores[(int)defenderTeam] += total;
                roundWinner = defenderTeam;
            }

            return new RoundResult(
                roundWinner, biddingTeam, contractMet,
                aRound, bRound,
                _matchScores[(int)TeamId.TeamA],
                _matchScores[(int)TeamId.TeamB]);
        }

        // ------------------------------------------------------------------ match queries

        public int    GetMatchScore(TeamId team) => _matchScores[(int)team];
        public bool   IsMatchOver()              => _matchScores[0] >= WinTarget || _matchScores[1] >= WinTarget;
        public TeamId GetMatchWinner()           => _matchScores[(int)TeamId.TeamA] >= WinTarget
                                                    ? TeamId.TeamA : TeamId.TeamB;
    }
}
