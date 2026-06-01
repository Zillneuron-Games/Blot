using System;
using Blot.AI;
using Blot.Cards;
using Blot.Players;

namespace Blot.Core.Managers
{
    public class MatchManager
    {
        public Player[] Players { get; }

        private Deck          _deck;
        private readonly Random _rng = new();

        public MatchManager()
        {
            Players = new Player[]
            {
                new HumanPlayer(0, "You",       TeamId.TeamA),
                new AIPlayer   (1, "CPU East",  TeamId.TeamB),
                new AIPlayer   (2, "CPU North", TeamId.TeamA),
                new AIPlayer   (3, "CPU West",  TeamId.TeamB),
            };
        }

        public HumanPlayer GetHumanPlayer() => (HumanPlayer)Players[0];

        public void CreateAndShuffleDeck()
        {
            _deck = new Deck();
            _deck.Shuffle(_rng);
        }

        public void DealCardsToPlayers()
        {
            foreach (var player in Players)
            {
                player.ClearHand();
                player.AddCards(_deck.Deal(8));
            }
        }

        public Suit SelectRandomTrump()      => (Suit)_rng.Next(0, 4);
        public int  SelectRandomLeadPlayer() => _rng.Next(0, 4);
    }
}
