using Blot.Core.Managers;

namespace Blot.Core.StateMachine
{
    /// <summary>
    /// Immutable bag of references injected into every state on Enter/Exit.
    /// States read from it but do not own the objects inside.
    /// </summary>
    public class GameContext
    {
        public MatchManager     MatchManager { get; }
        public RoundManager     RoundManager { get; }
        public ScoreManager     ScoreManager { get; }
        public GameStateMachine StateMachine { get; }

        public GameContext(
            MatchManager     match,
            RoundManager     round,
            ScoreManager     score,
            GameStateMachine fsm)
        {
            MatchManager = match;
            RoundManager = round;
            ScoreManager = score;
            StateMachine = fsm;
        }
    }
}
