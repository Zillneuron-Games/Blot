namespace Blot.Core.StateMachine
{
    public enum GameStateId
    {
        GameStart,
        DealCards,
        SelectTrump,   // kept for backward-compat; replaced in flow by Bidding
        Bidding,
        PlayTrick,
        EvaluateTrick,
        CheckRoundEnd,
        RoundEnd,
        CheckMatchEnd,
        MatchEnd
    }

    public interface IGameState
    {
        GameStateId StateId { get; }

        void Enter(GameContext context);
        void Exit(GameContext context);
    }
}
