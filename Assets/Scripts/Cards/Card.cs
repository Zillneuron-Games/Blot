namespace Blot.Cards
{
    public enum Suit { Clubs, Diamonds, Hearts, Spades, NoTrump }

    // Declared in a neutral order; actual game ranking is resolved in GetTrumpStrength / GetNonTrumpStrength
    public enum Rank { Seven, Eight, Nine, Ten, Jack, Queen, King, Ace }

    public class Card
    {
        public Suit Suit { get; }
        public Rank Rank { get; }

        public Card(Suit suit, Rank rank)
        {
            Suit = suit;
            Rank = rank;
        }

        // ------------------------------------------------------------------ points

        /// <summary>
        /// Returns the point value of this card for the given trump contract.
        ///
        /// NoTrump game (trump == Suit.NoTrump):
        ///   Ace=19, Ten=10, King=4, Queen=3, Jack=2, others=0
        ///   Total across 32 cards: 152
        ///
        /// Trump game:
        ///   Trump suit: Jack=20, Nine=14, Ace=11, Ten=10, King=4, Queen=3, others=0
        ///   Non-trump:  Ace=11, Ten=10, King=4, Queen=3, Jack=2, others=0
        ///   Total across 32 cards: 152
        /// </summary>
        public int GetPoints(Suit trump)
        {
            // NoTrump game: every card uses NoTrump values regardless of suit.
            if (trump == Suit.NoTrump)
            {
                return Rank switch
                {
                    Rank.Ace   => 19,
                    Rank.Ten   => 10,
                    Rank.King  => 4,
                    Rank.Queen => 3,
                    Rank.Jack  => 2,
                    _          => 0   // Nine, Eight, Seven
                };
            }

            if (Suit == trump)
            {
                return Rank switch
                {
                    Rank.Jack  => 20,
                    Rank.Nine  => 14,
                    Rank.Ace   => 11,
                    Rank.Ten   => 10,
                    Rank.King  => 4,
                    Rank.Queen => 3,
                    _          => 0   // Eight, Seven
                };
            }

            return Rank switch
            {
                Rank.Ace   => 11,
                Rank.Ten   => 10,
                Rank.King  => 4,
                Rank.Queen => 3,
                Rank.Jack  => 2,
                _          => 0   // Nine, Eight, Seven
            };
        }

        // ------------------------------------------------------------------ ranking
        /// <summary>Relative strength when this card IS the trump suit. Higher = stronger.</summary>
        public int GetTrumpStrength() => Rank switch
        {
            Rank.Jack  => 7,
            Rank.Nine  => 6,
            Rank.Ace   => 5,
            Rank.Ten   => 4,
            Rank.King  => 3,
            Rank.Queen => 2,
            Rank.Eight => 1,
            Rank.Seven => 0,
            _          => 0
        };

        /// <summary>Relative strength when this card is NOT the trump suit. Higher = stronger.</summary>
        public int GetNonTrumpStrength() => Rank switch
        {
            Rank.Ace   => 7,
            Rank.Ten   => 6,
            Rank.King  => 5,
            Rank.Queen => 4,
            Rank.Jack  => 3,
            Rank.Nine  => 2,
            Rank.Eight => 1,
            Rank.Seven => 0,
            _          => 0
        };

        public override string ToString() => $"{Rank} of {Suit}";
    }
}
