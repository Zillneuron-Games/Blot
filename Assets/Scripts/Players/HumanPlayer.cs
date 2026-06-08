using System;
using System.Collections.Generic;
using Blot.Bidding;
using Blot.Cards;
using Blot.Declarations;
using Blot.Gameplay;

namespace Blot.Players
{
    public class HumanPlayer : Player
    {
        // ---- card selection ------------------------------------------------
        public event Action<List<Card>> OnCardSelectionRequested;

        private Trick                 _pendingTrick;
        private Suit                  _pendingTrump;
        private IReadOnlyList<Player> _pendingAllPlayers;
        private bool                  _waitingForInput;

        // ---- bidding -------------------------------------------------------
        /// <summary>
        /// UI subscribes here to show the bidding panel.
        /// Receives minimum bid value. Check <see cref="CanChallengeNow"/> for challenge eligibility.
        /// </summary>
        public event Action<int> OnBidRequested;

        private bool _waitingForBid;
        public  int  MinimumBid { get; private set; }

        /// <summary>Fallback: auto-bid randomly when no UI handler is connected.</summary>
        public bool AutoBidFallback { get; set; } = true;

        // ---- challenge response --------------------------------------------
        /// <summary>UI subscribes here to show the "I'm Sure / Pass" response panel.</summary>
        public event Action OnSureResponseRequested;

        private bool _waitingForSureResponse;

        // ---- declarations --------------------------------------------------
        /// <summary>
        /// UI subscribes here to show the declaration announcement panel.
        /// The argument is all available declarations detected in this player's hand.
        /// </summary>
        public event Action<List<Declaration>> OnDeclareRequested;

        /// <summary>
        /// UI subscribes here to show the reveal confirmation panel.
        /// The argument is the declarations the player announced.
        /// </summary>
        public event Action<List<Declaration>> OnRevealRequested;

        private bool _waitingForDeclare;
        private bool _waitingForReveal;

        /// <summary>Fallback: auto-announce nothing and auto-confirm reveals when no UI is wired.</summary>
        public bool AutoDeclareFallback { get; set; } = true;

        // ------------------------------------------------------------------ ctor
        public HumanPlayer(int id, string name, TeamId team) : base(id, name, team) { }

        // ================================================================== card play

        public override void RequestPlay(Trick currentTrick, Suit trump, IReadOnlyList<Player> allPlayers)
        {
            _pendingTrick      = currentTrick;
            _pendingTrump      = trump;
            _pendingAllPlayers = allPlayers;
            _waitingForInput   = true;

            var valid = GetValidCards(currentTrick, trump, allPlayers);
            OnCardSelectionRequested?.Invoke(valid);
        }

        public bool TrySelectCard(Card card)
        {
            if (!_waitingForInput) return false;

            var valid = GetValidCards(_pendingTrick, _pendingTrump, _pendingAllPlayers);
            if (!valid.Contains(card)) return false;

            _waitingForInput = false;
            CommitCard(card);
            return true;
        }

        // ================================================================== bidding

        public override void RequestBid(int minimumBid)
        {
            MinimumBid     = minimumBid;
            _waitingForBid = true;
            OnBidRequested?.Invoke(minimumBid);

            if (AutoBidFallback)
            {
                Bid bid = UnityEngine.Random.value > 0.5f
                    ? null
                    : new Bid(minimumBid, (Suit)UnityEngine.Random.Range(0, 5));
                TryPlaceBid(bid);
            }
        }

        public bool TryPlaceBid(Bid bid)
        {
            if (!_waitingForBid) return false;
            _waitingForBid = false;
            CanChallengeNow = false;
            CommitBid(bid);
            return true;
        }

        // ================================================================== challenge response

        public override void RequestSureResponse()
        {
            _waitingForSureResponse = true;
            OnSureResponseRequested?.Invoke();

            if (AutoBidFallback)
            {
                // Default: decline (Pass) — the safer auto-choice.
                TrySureResponse(false);
            }
        }

        /// <summary>
        /// Called by the UI when the human responds to a challenge.
        /// true = "I'm Sure", false = Pass.
        /// Returns false if not currently waiting for a sure response.
        /// </summary>
        public bool TrySureResponse(bool isSure)
        {
            if (!_waitingForSureResponse) return false;
            _waitingForSureResponse = false;
            CommitSureResponse(isSure);
            return true;
        }

        // ================================================================== declarations

        public override void RequestDeclare(Suit trump)
        {
            _waitingForDeclare = true;

            var available = DeclarationDetector.FindAll(Hand);

            // No declarations in hand — skip the UI entirely and auto-commit nothing.
            if (available.Count == 0)
            {
                UnityEngine.Debug.Log("[Declarations] Human has no declarations, skipping declaration UI");
                TryAnnounce(new List<Declaration>());
                return;
            }

            OnDeclareRequested?.Invoke(available);

            if (AutoDeclareFallback)
            {
                // Default: announce nothing (safe, strategic silence).
                TryAnnounce(new List<Declaration>());
            }
        }

        /// <summary>
        /// Called by the UI when the human confirms their announcement.
        /// <paramref name="chosen"/> must be a non-overlapping subset of available declarations.
        /// Returns false if not currently waiting for declaration input.
        /// </summary>
        public bool TryAnnounce(List<Declaration> chosen)
        {
            if (!_waitingForDeclare) return false;
            _waitingForDeclare = false;
            CommitDeclarations(chosen);
            return true;
        }

        public override void RequestReveal(List<Declaration> toReveal)
        {
            _waitingForReveal = true;
            OnRevealRequested?.Invoke(toReveal);

            if (AutoDeclareFallback)
            {
                // Default: auto-confirm reveal (standard behavior).
                TryReveal(true);
            }
        }

        /// <summary>
        /// Called by the UI when the human confirms or skips their reveal.
        /// Returns false if not currently waiting for reveal input.
        /// </summary>
        public bool TryReveal(bool confirmed)
        {
            if (!_waitingForReveal) return false;
            _waitingForReveal = false;
            CommitReveal(confirmed);
            return true;
        }
    }
}
