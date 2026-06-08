using System.Collections.Generic;
using Blot.Cards;

namespace Blot.Declarations
{
    public enum DeclarationType { Terz, Fifty, OneHundred, FourOfAKind }

    /// <summary>
    /// A single declaration (Terz / Fifty / OneHundred / FourOfAKind) held by a player.
    /// null is never used — an empty List&lt;Declaration&gt; means "no declarations".
    ///
    /// GetStrengthRank(trump) returns a number 1–11 that maps directly to the spec
    /// ranking table.  Lower number = stronger declaration.
    /// </summary>
    public class Declaration
    {
        public DeclarationType Type     { get; }
        /// <summary>Only meaningful when Type == FourOfAKind.</summary>
        public Rank            FourRank { get; }
        /// <summary>The specific cards that form this declaration.</summary>
        public List<Card>      Cards    { get; }

        // ---- constructors --------------------------------------------------

        /// <summary>Sequence constructor (Terz / Fifty / OneHundred).</summary>
        public Declaration(DeclarationType type, List<Card> cards)
        {
            Type  = type;
            Cards = cards;
        }

        /// <summary>Four-of-a-kind constructor.</summary>
        public Declaration(Rank fourRank, List<Card> cards)
        {
            Type     = DeclarationType.FourOfAKind;
            FourRank = fourRank;
            Cards    = cards;
        }

        // ---- scoring -------------------------------------------------------

        /// <summary>
        /// Point value of this declaration.
        /// Four-of-a-kind values depend on whether the contract is NoTrump.
        /// </summary>
        public int GetValue(Suit trump)
        {
            switch (Type)
            {
                case DeclarationType.Terz:        return 20;
                case DeclarationType.Fifty:       return 50;
                case DeclarationType.OneHundred:  return 100;
                case DeclarationType.FourOfAKind: return GetFourValue(trump == Suit.NoTrump);
                default:                          return 0;
            }
        }

        private int GetFourValue(bool noTrump)
        {
            if (noTrump)
            {
                switch (FourRank)
                {
                    case Rank.Ace:   return 190;
                    case Rank.Ten:   return 100;
                    case Rank.King:  return 100;
                    case Rank.Queen: return 100;
                    case Rank.Jack:  return 100;
                    default:         return 0;   // Nine, Eight, Seven
                }
            }
            else
            {
                switch (FourRank)
                {
                    case Rank.Jack:  return 200;
                    case Rank.Nine:  return 140;
                    case Rank.Ace:   return 110;
                    case Rank.Ten:   return 100;
                    case Rank.King:  return 100;
                    case Rank.Queen: return 100;
                    default:         return 0;   // Eight, Seven
                }
            }
        }

        // ---- comparison ----------------------------------------------------

        /// <summary>
        /// Strength rank (1 = strongest, 11 = weakest).
        /// Maps exactly to the spec ranking table.
        ///
        /// NoTrump:  1=4Aces  2=4Tens  3=4Kings  4=4Queens  5=4Jacks
        ///            6=4Nines 7=4Eights 8=4Sevens 9=OneHundred 10=Fifty 11=Terz
        ///
        /// Trump:    1=4Jacks  2=4Nines  3=4Aces  4=4Kings  5=4Queens
        ///            6=4Tens  7=4Eights 8=4Sevens 9=OneHundred 10=Fifty 11=Terz
        /// </summary>
        public int GetStrengthRank(Suit trump)
        {
            switch (Type)
            {
                case DeclarationType.Terz:        return 11;
                case DeclarationType.Fifty:       return 10;
                case DeclarationType.OneHundred:  return 9;
                case DeclarationType.FourOfAKind: return GetFourStrengthRank(trump == Suit.NoTrump);
                default:                          return 99;
            }
        }

        private int GetFourStrengthRank(bool noTrump)
        {
            if (noTrump)
            {
                switch (FourRank)
                {
                    case Rank.Ace:   return 1;
                    case Rank.Ten:   return 2;
                    case Rank.King:  return 3;
                    case Rank.Queen: return 4;
                    case Rank.Jack:  return 5;
                    case Rank.Nine:  return 6;
                    case Rank.Eight: return 7;
                    case Rank.Seven: return 8;
                    default:         return 9;
                }
            }
            else
            {
                switch (FourRank)
                {
                    case Rank.Jack:  return 1;
                    case Rank.Nine:  return 2;
                    case Rank.Ace:   return 3;
                    case Rank.King:  return 4;
                    case Rank.Queen: return 5;
                    case Rank.Ten:   return 6;
                    case Rank.Eight: return 7;
                    case Rank.Seven: return 8;
                    default:         return 9;
                }
            }
        }

        // --------------------------------------------------------------------

        public override string ToString()
        {
            switch (Type)
            {
                case DeclarationType.Terz:        return "Terz";
                case DeclarationType.Fifty:       return "Fifty";
                case DeclarationType.OneHundred:  return "OneHundred";
                case DeclarationType.FourOfAKind: return $"Four {FourRank}s";
                default:                          return "Unknown";
            }
        }
    }
}
