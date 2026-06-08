using Blot.Players;

namespace Blot.Scoring
{
    public class RoundResult
    {
        // ---- round outcome -------------------------------------------------
        public TeamId Winner          { get; }
        public TeamId BiddingTeam     { get; }
        public bool   ContractMet     { get; }

        // ---- contract details ----------------------------------------------
        public int ContractBidValue     { get; }
        public int ContractTargetPoints { get; }
        public int BidderRoundScore     { get; }

        // ---- points scored this round (after bonuses, before redistribution)
        public int TeamAPoints     { get; }
        public int TeamBPoints     { get; }

        // ---- running match totals (include challenge bonus if any) ---------
        public int TeamAMatchTotal { get; }
        public int TeamBMatchTotal { get; }

        // ---- challenge bonus -----------------------------------------------
        /// <summary>Extra match points awarded to the round winner due to a challenge.</summary>
        public int ChallengeBonus           { get; }
        /// <summary>0 = no challenge, 1 = challenged only, 3 = challenged + I'm Sure.</summary>
        public int ChallengeBonusMultiplier { get; }

        public RoundResult(
            TeamId winner, TeamId biddingTeam, bool contractMet,
            int bidValue, int targetPoints, int bidderScore,
            int aPoints, int bPoints, int aTotal, int bTotal,
            int challengeBonus = 0, int challengeMultiplier = 0)
        {
            Winner                  = winner;
            BiddingTeam             = biddingTeam;
            ContractMet             = contractMet;
            ContractBidValue        = bidValue;
            ContractTargetPoints    = targetPoints;
            BidderRoundScore        = bidderScore;
            TeamAPoints             = aPoints;
            TeamBPoints             = bPoints;
            TeamAMatchTotal         = aTotal;
            TeamBMatchTotal         = bTotal;
            ChallengeBonus          = challengeBonus;
            ChallengeBonusMultiplier = challengeMultiplier;
        }
    }
}
