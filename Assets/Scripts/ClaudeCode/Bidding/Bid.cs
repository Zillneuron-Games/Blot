using Blot.Cards;

namespace Blot.Bidding
{
    /// <summary>
    /// Represents a single action during the bidding/negotiation phase.
    /// null   = Pass.
    /// Normal = Value (8–16) + Suit (including NoTrump).
    /// Kaput  = IsKaput == true; EffectiveBidValue = 25 + KaputExtraValue.
    /// Challenge = IsChallenge == true  ("I Don't Believe").
    ///
    /// Kaput rules:
    ///   • EffectiveBidValue = 25 + KaputExtraValue.
    ///   • Once a Kaput bid exists, only a higher-value Kaput can beat it.
    ///   • A normal numeric bid cannot beat any Kaput bid.
    ///
    /// Use <see cref="MakeChallenge"/> to create a challenge bid.
    /// Use <see cref="MakeKaput"/> to create a Kaput bid.
    /// </summary>
    public class Bid
    {
        public int  Value           { get; }
        public Suit Suit            { get; }
        /// <summary>True when this bid is an "I Don't Believe" challenge.</summary>
        public bool IsChallenge     { get; }
        /// <summary>True when this is a Kaput contract bid.</summary>
        public bool IsKaput         { get; }
        /// <summary>Additional bonus value on top of the base Kaput value of 25.</summary>
        public int  KaputExtraValue { get; }

        /// <summary>
        /// Effective bid value used for comparison.
        /// Normal bids: Value (8–16).
        /// Kaput bids:  25 + KaputExtraValue.
        /// </summary>
        public int EffectiveBidValue => IsKaput ? 25 + KaputExtraValue : Value;

        /// <summary>Normal bid constructor (value 8–16).</summary>
        public Bid(int value, Suit suit)
        {
            Value           = value;
            Suit            = suit;
            IsChallenge     = false;
            IsKaput         = false;
            KaputExtraValue = 0;
        }

        /// <summary>Kaput bid constructor. EffectiveBidValue = 25 + kaputExtra.</summary>
        public Bid(Suit suit, int kaputExtra = 0)
        {
            Value           = 25 + kaputExtra;
            Suit            = suit;
            IsChallenge     = false;
            IsKaput         = true;
            KaputExtraValue = kaputExtra;
        }

        private Bid(bool challenge) { IsChallenge = challenge; }

        /// <summary>Creates an "I Don't Believe" challenge action.</summary>
        public static Bid MakeChallenge() => new Bid(true);

        /// <summary>Creates a Kaput bid with optional extra value.</summary>
        public static Bid MakeKaput(Suit suit, int kaputExtra = 0) => new Bid(suit, kaputExtra);

        public override string ToString() =>
            IsChallenge ? "I Don't Believe!"
            : IsKaput   ? (KaputExtraValue > 0
                              ? $"Kaput +{KaputExtraValue} {Suit}"
                              : $"Kaput {Suit}")
                        : $"{Value} {Suit}";
    }
}
