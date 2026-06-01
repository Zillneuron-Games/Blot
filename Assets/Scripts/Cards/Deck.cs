using System;
using System.Collections.Generic;

namespace Blot.Cards
{
    public class Deck
    {
        private readonly List<Card> _cards = new();

        public int Count => _cards.Count;

        public Deck()
        {
            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
                foreach (Rank rank in Enum.GetValues(typeof(Rank)))
                    _cards.Add(new Card(suit, rank));
        }

        public void Shuffle(Random rng)
        {
            for (int i = _cards.Count - 1; i > 0; i--)
            {
                int j = rng.Next(0, i + 1);
                (_cards[i], _cards[j]) = (_cards[j], _cards[i]);
            }
        }

        /// <summary>Deals the top <paramref name="count"/> cards and removes them from the deck.</summary>
        public List<Card> Deal(int count)
        {
            var hand = _cards.GetRange(0, count);
            _cards.RemoveRange(0, count);
            return hand;
        }
    }
}
