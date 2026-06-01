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
        /// <summary>True if the bidding team reached their contract target.</summary>
        public bool   ContractMet     { get; }

        // ---- contract details ----------------------------------------------
        /// <summary>The bid value (e.g. 10). Target = BidValue * 10.</summary>
        public int ContractBidValue     { get; }
        /// <summary>Minimum points the bidding team needed (BidValue * 10).</summary>
        public int ContractTargetPoints { get; }
        /// <summary>Actual points the bidding team scored this round.</summary>
        public int BidderRoundScore     { get; }

        // ---- points scored this round (after bonuses, before redistribution) --
        public int TeamAPoints     { get; }
        public int TeamBPoints     { get; }

        // ---- running match totals ------------------------------------------
        public int TeamAMatchTotal { get; }
        public int TeamBMatchTotal { get; }

        public RoundResult(
            TeamId winner, TeamId biddingTeam, bool contractMet,
            int bidValue, int targetPoints, int bidderScore,
            int aPoints, int bPoints, int aTotal, int bTotal)
        {
            Winner              = winner;
            BiddingTeam         = biddingTeam;
            ContractMet         = contractMet;
            ContractBidValue    = bidValue;
            ContractTargetPoints = targetPoints;
            BidderRoundScore    = bidderScore;
            TeamAPoints         = aPoints;
            TeamBPoints         = bPoints;
            TeamAMatchTotal     = aTotal;
            TeamBMatchTotal     = bTotal;
        }
    }
}
