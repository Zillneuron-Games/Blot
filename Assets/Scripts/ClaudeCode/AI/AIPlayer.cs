using System.Collections.Generic;
using System.Text;
using Blot.Bidding;
using Blot.Cards;
using Blot.Declarations;
using Blot.Gameplay;
using Blot.Gameplay.Events;
using Blot.Players;
using UnityEngine;

namespace Blot.AI
{
    /// <summary>
    /// AI player that uses:
    ///   - Convention-based bidding (AIBiddingAdvisor + AIHandEvaluator)
    ///   - Monte Carlo card-play selection (MonteCarloCardPlayAI)
    ///
    /// Knowledge model:
    ///   Subscribes to global GameEvents to accumulate public information:
    ///     OnCardsDealt  → reset knowledge for new round
    ///     OnCardPlayed  → record every played card
    ///     OnBidPlaced   → record every bid or pass
    ///
    ///   Never accesses opponent hands or any hidden data.
    /// </summary>
    public class AIPlayer : Player
    {
        private readonly AIKnowledgeBase      _knowledge  = new();
        private readonly MonteCarloCardPlayAI _monteCarlo = new();

        public AIPlayer(int id, string name, TeamId team) : base(id, name, team)
        {
            GameEvents.OnCardsDealt += HandleCardsDealt;
            GameEvents.OnCardPlayed += HandleCardPlayed;
            GameEvents.OnBidPlaced  += HandleBidPlaced;
        }

        // ---- GameEvent handlers --------------------------------------------

        private void HandleCardsDealt()                    => _knowledge.Reset();
        private void HandleCardPlayed(Player p, Card c)    => _knowledge.RecordCardPlayed(c);
        private void HandleBidPlaced(Player p, Bid b)      => _knowledge.RecordBid(p, b);

        // ------------------------------------------------------------------ card play

        public override void RequestPlay(Trick currentTrick, Suit trump, IReadOnlyList<Player> allPlayers)
        {
            var valid = GetValidCards(currentTrick, trump, allPlayers);

            int mySeat = FindSeat(allPlayers, this);

            Debug.Log($"[AI Play] {Name}: {valid.Count} valid: {ListCards(valid)}");

            var chosen = _monteCarlo.ChooseCard(
                valid, Hand, trump, currentTrick, allPlayers, _knowledge, mySeat, Team);

            Debug.Log($"[AI Play] {Name}: Playing {chosen}");
            CommitCard(chosen);
        }

        // ------------------------------------------------------------------ declarations

        public override void RequestDeclare(Suit trump)
        {
            var optimal = DeclarationDetector.FindOptimal(Hand, trump);
            CommitDeclarations(optimal);
        }

        public override void RequestReveal(List<Declaration> toReveal)
        {
            // Always reveal if we have declarations to show.
            CommitReveal(toReveal.Count > 0);
        }

        // ------------------------------------------------------------------ bidding

        public override void RequestBid(int minimumBid)
        {
            // Read flags before clearing them (BiddingState clears them too, but we clear defensively).
            bool canChallenge = CanChallengeNow;
            bool onlyKaput    = OnlyKaputBidAllowed;
            CanChallengeNow     = false;
            OnlyKaputBidAllowed = false;

            Debug.Log($"[AI Bid] {Name}: min={minimumBid} canChallenge={canChallenge} " +
                      $"onlyKaput={onlyKaput} teamStmts={_knowledge.GetTeamStatementCount(Team)}");

            var bid = AIBiddingAdvisor.ChooseBid(
                Hand, minimumBid, canChallenge, onlyKaput, _knowledge, Team, Name);

            CommitBid(bid);
        }

        // ------------------------------------------------------------------ challenge response

        public override void RequestSureResponse()
        {
            // The current winning bid is ours (we are the contract owner being challenged).
            var winBid  = _knowledge.GetCurrentWinningBid();
            Suit trump  = winBid?.Bid?.Suit ?? Suit.Clubs;
            int  bidVal = winBid?.Bid?.EffectiveBidValue ?? 8;

            bool isSure = AIBiddingAdvisor.ChooseSureResponse(Hand, trump, bidVal, Name);
            CommitSureResponse(isSure);
        }

        // ------------------------------------------------------------------ helpers

        private static int FindSeat(IReadOnlyList<Player> players, Player target)
        {
            for (int i = 0; i < players.Count; i++)
                if (players[i] == target) return i;
            return 0;
        }

        private static string ListCards(List<Card> cards)
        {
            var sb = new StringBuilder();
            foreach (var c in cards) { sb.Append(c); sb.Append(' '); }
            return sb.ToString();
        }
    }
}
