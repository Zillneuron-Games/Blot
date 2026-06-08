using System;
using System.Collections.Generic;
using Blot.AI;
using Blot.Cards;
using Blot.Players;
using UnityEngine;
using Random = System.Random;

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

            ValidateDealtHands();
        }

        /// <summary>
        /// After dealing, verifies each player has exactly 8 cards and that
        /// no (Suit, Rank) combination appears in more than one hand.
        /// Logs errors to the Console without throwing so gameplay can continue.
        /// </summary>
        private void ValidateDealtHands()
        {
            bool hasError = false;
            var  seen     = new HashSet<string>(32);

            foreach (var player in Players)
            {
                if (player.Hand.Count != 8)
                {
                    Debug.LogError($"[Deal] Player{player.Id} ({player.Name}) has " +
                                   $"{player.Hand.Count} cards — expected 8!");
                    hasError = true;
                }

                foreach (var card in player.Hand)
                {
                    // Guard: no card should have NoTrump as its suit.
                    if (card.Suit == Suit.NoTrump)
                    {
                        Debug.LogError($"[Deal] Player{player.Id} received a card with " +
                                       $"Suit.NoTrump ({card}). Deck was built incorrectly!");
                        hasError = true;
                    }

                    string key = $"{(int)card.Suit}_{(int)card.Rank}";
                    if (!seen.Add(key))
                    {
                        Debug.LogError($"[Deal] Duplicate card across hands: {card} " +
                                       $"already seen before Player{player.Id}'s hand!");
                        hasError = true;
                    }
                }
            }

            if (!hasError)
                Debug.Log("[Deal] Validation OK — 4 × 8 cards, all 32 unique, no NoTrump suits.");
        }

        public Suit SelectRandomTrump()      => (Suit)_rng.Next(0, 4);
        public int  SelectRandomLeadPlayer() => _rng.Next(0, 4);
    }
}
