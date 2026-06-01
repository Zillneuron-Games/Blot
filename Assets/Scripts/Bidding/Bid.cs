using Blot.Cards;

namespace Blot.Bidding
{
    /// <summary>
    /// Represents a single bid during the negotiation phase.
    /// null = Pass.
    ///
    /// Value = bid level (8–16). Target score = Value * 10.
    /// Suit  = trump choice, including NoTrump.
    /// </summary>
    public class Bid
    {
        public int  Value { get; }
        public Suit Suit  { get; }

        public Bid(int value, Suit suit)
        {
            Value = value;
            Suit  = suit;
        }

        public override string ToString() => $"{Value} {Suit}";
    }
}
