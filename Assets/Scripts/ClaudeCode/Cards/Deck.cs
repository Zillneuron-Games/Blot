using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace Blot.Cards
{
    public class Deck
    {
        /// <summary>
        /// The four real card suits.
        /// NoTrump is a CONTRACT option only — it is never a card's suit.
        /// Using an explicit array (instead of Enum.GetValues) guards against
        /// future enum additions accidentally inflating the deck.
        /// </summary>
        private static readonly Suit[] CardSuits =
        {
            Suit.Clubs, Suit.Diamonds, Suit.Hearts, Suit.Spades
        };

        private readonly List<Card> _cards = new();

        public int Count => _cards.Count;

        public Deck()
        {
            foreach (Suit suit in CardSuits)
                foreach (Rank rank in Enum.GetValues(typeof(Rank)))
                    _cards.Add(new Card(suit, rank));

            AssertUnique("after creation");
        }

        public void Shuffle(Random rng)
        {
            for (int i = _cards.Count - 1; i > 0; i--)
            {
                int j = rng.Next(0, i + 1);
                (_cards[i], _cards[j]) = (_cards[j], _cards[i]);
            }

            AssertUnique("after shuffle");
        }

        /// <summary>Deals the top <paramref name="count"/> cards and removes them from the deck.</summary>
        public List<Card> Deal(int count)
        {
            var hand = _cards.GetRange(0, count);
            _cards.RemoveRange(0, count);
            return hand;
        }

        // ==================================================================
        // Validation
        // ==================================================================

        /// <summary>
        /// Asserts exactly 32 unique (Suit, Rank) cards are present.
        /// Logs an error (not an exception) so the game can continue while
        /// the problem is diagnosed in the Console.
        /// </summary>
        private void AssertUnique(string context)
        {
            const int ExpectedCount = 32;   // 4 suits × 8 ranks

            if (_cards.Count != ExpectedCount)
            {
                Debug.LogError($"[Deck] {context}: Expected {ExpectedCount} cards, " +
                               $"found {_cards.Count}. Check for NoTrump or extra suits in enum.");
            }

            var seen = new HashSet<string>(_cards.Count);
            foreach (var card in _cards)
            {
                string key = $"{(int)card.Suit}_{(int)card.Rank}";
                if (!seen.Add(key))
                    Debug.LogError($"[Deck] {context}: Duplicate card detected — {card}!");
            }

            if (_cards.Count == ExpectedCount && seen.Count == ExpectedCount)
                Debug.Log($"[Deck] Validation OK {context}: {ExpectedCount} unique cards.");
        }
    }
}
