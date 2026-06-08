using Blot.Bidding;
using Blot.Cards;
using Blot.Core.Managers;
using Blot.Core.StateMachine;
using Blot.Declarations;
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

        public MatchManager        MatchManager        { get; private set; }
        public RoundManager        RoundManager        { get; private set; }
        public ScoreManager        ScoreManager        { get; private set; }
        public DeclarationManager  DeclarationManager  { get; private set; }
        public GameStateMachine    StateMachine        { get; private set; }

        public static GameManager Instance { get; private set; }

        // ------------------------------------------------------------------ lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            BuildManagers();
            BuildStateMachine();
        }

        private void Start()
        {
            StateMachine.Start(GameStateId.GameStart);
        }

        // ------------------------------------------------------------------ setup

        private void BuildManagers()
        {
            MatchManager       = new MatchManager();
            RoundManager       = new RoundManager(MatchManager.Players);
            ScoreManager       = new ScoreManager();
            DeclarationManager = new DeclarationManager();
        }

        private void BuildStateMachine()
        {
            StateMachine = new GameStateMachine();

            var context = new GameContext(
                MatchManager, RoundManager, ScoreManager, StateMachine, DeclarationManager);
            StateMachine.Initialize(context);

            StateMachine.RegisterState(new GameStartState());
            StateMachine.RegisterState(new DealCardsState());
            StateMachine.RegisterState(new SelectTrumpState());          // legacy; not reached in normal flow
            StateMachine.RegisterState(new BiddingState());
            StateMachine.RegisterState(new AnnounceDeclarationsState()); // new
            StateMachine.RegisterState(new RevealDeclarationsState());   // new
            StateMachine.RegisterState(new PlayTrickState());
            StateMachine.RegisterState(new EvaluateTrickState());
            StateMachine.RegisterState(new CheckRoundEndState());
            StateMachine.RegisterState(new RoundEndState());
            StateMachine.RegisterState(new CheckMatchEndState());
            StateMachine.RegisterState(new MatchEndState());
        }

        // ------------------------------------------------------------------ debug helpers

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

        public void RestartMatch()
        {
            ScoreManager       = new ScoreManager();
            RoundManager       = new RoundManager(MatchManager.Players);
            DeclarationManager = new DeclarationManager();

            var context = new GameContext(
                MatchManager, RoundManager, ScoreManager, StateMachine, DeclarationManager);
            StateMachine.Initialize(context);

            GameEvents.GameRestarted();
            StateMachine.Start(GameStateId.GameStart);
        }
    }
}
