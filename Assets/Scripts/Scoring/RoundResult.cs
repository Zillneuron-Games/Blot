using Blot.Players;

namespace Blot.Scoring
{
    public class RoundResult
    {
        // ---- round outcome -------------------------------------------------
        /// <summary>Team that wins the round (contract-aware).</summary>
        public TeamId Winner          { get; }
        /// <summary>Team that made the winning bid this round.</summary>
        public TeamId BiddingTeam     { get; }
        /// <summary>True if the bidding team scored more points than the defending team.</summary>
        public bool   ContractMet     { get; }

        // ---- points scored this round (after bonuses, before contract redistribution) --
        public int TeamAPoints     { get; }
        public int TeamBPoints     { get; }

        // ---- running match totals ------------------------------------------
        public int TeamAMatchTotal { get; }
        public int TeamBMatchTotal { get; }

        public RoundResult(
            TeamId winner, TeamId biddingTeam, bool contractMet,
            int aPoints, int bPoints, int aTotal, int bTotal)
        {
            Winner          = winner;
            BiddingTeam     = biddingTeam;
            ContractMet     = contractMet;
            TeamAPoints     = aPoints;
            TeamBPoints     = bPoints;
            TeamAMatchTotal = aTotal;
            TeamBMatchTotal = bTotal;
        }
    }
}
