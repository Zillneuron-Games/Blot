using Blot.Cards;

namespace Blot.Scoring
{
    /// <summary>
    /// Carries all context needed by the round result panel.
    /// Built in RoundEndState before AdvanceRoundStarter() is called, so
    /// RoundManager data is still valid when the panel reads it.
    /// </summary>
    public class RoundEndData
    {
        public RoundResult Result             { get; }
        public int    TeamADeclarationPts     { get; }   // raw declaration bonus before conversion
        public int    TeamBDeclarationPts     { get; }
        public string BidderName              { get; }
        public Suit   Trump                   { get; }
        public bool   IsKaput                 { get; }
        public int    ContractBidValue        { get; }   // same as Result.ContractBidValue
        public int    ContractTargetPoints    { get; }   // same as Result.ContractTargetPoints

        public RoundEndData(
            RoundResult result,
            int teamADeclPts,
            int teamBDeclPts,
            string bidderName,
            Suit trump,
            bool isKaput,
            int contractBidValue,
            int contractTargetPoints)
        {
            Result              = result;
            TeamADeclarationPts = teamADeclPts;
            TeamBDeclarationPts = teamBDeclPts;
            BidderName          = bidderName;
            Trump               = trump;
            IsKaput             = isKaput;
            ContractBidValue    = contractBidValue;
            ContractTargetPoints = contractTargetPoints;
        }
    }
}
