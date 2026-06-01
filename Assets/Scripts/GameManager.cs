using Blot.Bidding;
using Blot.Cards;
using Blot.Core.Managers;
using Blot.Core.StateMachine;
using Blot.Gameplay.Events;
using Blot.Gameplay.States;
using UnityEngine;

namespace Blot
{
    /// <summary>
    /// Scene entry point (MonoBehaviour).
    /// Creates all pure-C# managers, wires the FSM, and kicks off the match.
    /// Referenced by UI components via GameManager.Instance.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField] private bool _logStateTransitions = true;

        public MatchManager     MatchManager { get; private set; }
        public RoundManager     RoundManager { get; private set; }
        public ScoreManager     ScoreManager { get; private set; }
        public GameStateMachine StateMachine { get; private set; }

        public static GameManager Instance { get; private set; }

        // ------------------------------------------------------------------ lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            // Build managers and FSM in Awake so they exist before any other
            // component's Start() runs (AIHandView, GameUIManager, etc.).
            BuildManagers();
            BuildStateMachine();
        }

        private void Start()
        {
            // Fire the FSM after all other Start() subscriptions are in place.
            // Script Execution Order is set to 100 (runs last) by BuildGameScene.
            StateMachine.Start(GameStateId.GameStart);
        }

        // ------------------------------------------------------------------ setup

        private void BuildManagers()
        {
            MatchManager = new MatchManager();
            RoundManager = new RoundManager(MatchManager.Players);
            ScoreManager = new ScoreManager();
        }

        private void BuildStateMachine()
        {
            StateMachine = new GameStateMachine();

            var context = new GameContext(MatchManager, RoundManager, ScoreManager, StateMachine);
            StateMachine.Initialize(context);

            StateMachine.RegisterState(new GameStartState());
            StateMachine.RegisterState(new DealCardsState());
            StateMachine.RegisterState(new SelectTrumpState()); // kept; no longer reached in normal flow
            StateMachine.RegisterState(new BiddingState());
            StateMachine.RegisterState(new PlayTrickState());
            StateMachine.RegisterState(new EvaluateTrickState());
            StateMachine.RegisterState(new CheckRoundEndState());
            StateMachine.RegisterState(new RoundEndState());
            StateMachine.RegisterState(new CheckMatchEndState());
            StateMachine.RegisterState(new MatchEndState());
        }

        // ------------------------------------------------------------------ public API

        // ------------------------------------------------------------------ debug helpers

        /// <summary>
        /// Force the human player to bid the specified suit.
        /// Use via the Inspector context menu or a UI debug button.
        /// Only works while the game is in the Bidding state.
        /// </summary>
        [ContextMenu("Debug: Bid Clubs")]
        public void DebugBidClubs()    { var h = MatchManager.GetHumanPlayer(); h.TryPlaceBid(new Bid(h.MinimumBid, Suit.Clubs)); }
        [ContextMenu("Debug: Bid Diamonds")]
        public void DebugBidDiamonds() { var h = MatchManager.GetHumanPlayer(); h.TryPlaceBid(new Bid(h.MinimumBid, Suit.Diamonds)); }
        [ContextMenu("Debug: Bid Hearts")]
        public void DebugBidHearts()   { var h = MatchManager.GetHumanPlayer(); h.TryPlaceBid(new Bid(h.MinimumBid, Suit.Hearts)); }
        [ContextMenu("Debug: Bid Spades")]
        public void DebugBidSpades()   { var h = MatchManager.GetHumanPlayer(); h.TryPlaceBid(new Bid(h.MinimumBid, Suit.Spades)); }
        [ContextMenu("Debug: Bid NoTrump")]
        public void DebugBidNoTrump()  { var h = MatchManager.GetHumanPlayer(); h.TryPlaceBid(new Bid(h.MinimumBid, Suit.NoTrump)); }
        [ContextMenu("Debug: Pass Bid")]
        public void DebugPassBid()     => MatchManager.GetHumanPlayer().TryPlaceBid(null);

        // ------------------------------------------------------------------ match API

        /// <summary>Called by the UI restart button after a match ends.</summary>
        public void RestartMatch()
        {
            // Replace managers so all scores reset cleanly
            ScoreManager = new ScoreManager();
            RoundManager = new RoundManager(MatchManager.Players);

            var context = new GameContext(MatchManager, RoundManager, ScoreManager, StateMachine);
            StateMachine.Initialize(context);

            GameEvents.GameRestarted();
            StateMachine.Start(GameStateId.GameStart);
        }
    }
}
