using System.Collections.Generic;
using Blot.Cards;

namespace Blot.Declarations
{
    /// <summary>
    /// Stateless helper that scans a player's hand for valid declarations.
    ///
    /// FindAll   — returns every possible declaration (cards may overlap between entries).
    ///             Use this to populate a human-player selection UI.
    ///
    /// FindOptimal — greedy non-overlapping set, sorted strongest-first.
    ///               Use this for AI automatic announcement.
    ///
    /// IsNonOverlapping — validate that a human-chosen subset has no card reuse.
    /// </summary>
    public static class DeclarationDetector
    {
        // ==================================================================
        // Public API
        // ==================================================================

        /// <summary>
        /// Returns every individual declaration that exists in <paramref name="hand"/>.
        /// A single card may appear in multiple results (e.g. J♠ in both a Fifty and a Terz).
        /// </summary>
        public static List<Declaration> FindAll(IReadOnlyList<Card> hand)
        {
            var result = new List<Declaration>();
            FindSequences(hand, result);
            FindFourOfAKind(hand, result);
            return result;
        }

        /// <summary>
        /// Greedy algorithm: picks the strongest declaration first, skips any whose
        /// cards overlap with already-chosen cards. Optimal for AI use.
        /// </summary>
        public static List<Declaration> FindOptimal(IReadOnlyList<Card> hand, Suit trump)
        {
            var all = FindAll(hand);

            // Sort ascending by strength rank (lower rank = stronger = pick first).
            all.Sort((a, b) => a.GetStrengthRank(trump).CompareTo(b.GetStrengthRank(trump)));

            var chosen    = new List<Declaration>();
            var usedCards = new HashSet<Card>();   // Card has no Equals override → reference equality

            foreach (var decl in all)
            {
                bool overlap = false;
                foreach (var card in decl.Cards)
                {
                    if (usedCards.Contains(card)) { overlap = true; break; }
                }
                if (overlap) continue;

                chosen.Add(decl);
                foreach (var card in decl.Cards) usedCards.Add(card);
            }

            return chosen;
        }

        /// <summary>
        /// Returns true when no card appears more than once across all declarations
        /// in <paramref name="declarations"/>.
        /// </summary>
        public static bool IsNonOverlapping(IEnumerable<Declaration> declarations)
        {
            var seen = new HashSet<Card>();   // reference equality
            foreach (var decl in declarations)
                foreach (var card in decl.Cards)
                    if (!seen.Add(card)) return false;
            return true;
        }

        // ==================================================================
        // Private — sequence detection
        // ==================================================================

        private static void FindSequences(IReadOnlyList<Card> hand, List<Declaration> result)
        {
            // Group cards by suit (NoTrump suit guard: no card has it, but guard anyway).
            var bySuit = new Dictionary<Suit, Dictionary<int, Card>>();

            foreach (var card in hand)
            {
                if (card.Suit == Suit.NoTrump) continue;

                if (!bySuit.ContainsKey(card.Suit))
                    bySuit[card.Suit] = new Dictionary<int, Card>();

                bySuit[card.Suit][(int)card.Rank] = card;
            }

            foreach (var kvp in bySuit)
            {
                var rankToCard = kvp.Value;

                // Window sizes: 5 (OneHundred), 4 (Fifty), 3 (Terz).
                // Rank values: Seven=0 … Ace=7 (8 possible values).
                for (int windowLen = 5; windowLen >= 3; windowLen--)
                {
                    var type = windowLen == 5 ? DeclarationType.OneHundred
                             : windowLen == 4 ? DeclarationType.Fifty
                                              : DeclarationType.Terz;

                    // Starting rank can be 0 to (8 - windowLen).
                    for (int start = 0; start <= 8 - windowLen; start++)
                    {
                        bool allPresent = true;
                        for (int i = 0; i < windowLen; i++)
                        {
                            if (!rankToCard.ContainsKey(start + i)) { allPresent = false; break; }
                        }
                        if (!allPresent) continue;

                        var cards = new List<Card>(windowLen);
                        for (int i = 0; i < windowLen; i++)
                            cards.Add(rankToCard[start + i]);

                        result.Add(new Declaration(type, cards));
                    }
                }
            }
        }

        // ==================================================================
        // Private — four-of-a-kind detection
        // ==================================================================

        private static void FindFourOfAKind(IReadOnlyList<Card> hand, List<Declaration> result)
        {
            var byRank = new Dictionary<Rank, List<Card>>();

            foreach (var card in hand)
            {
                if (!byRank.ContainsKey(card.Rank))
                    byRank[card.Rank] = new List<Card>();
                byRank[card.Rank].Add(card);
            }

            foreach (var kvp in byRank)
            {
                if (kvp.Value.Count >= 4)
                    result.Add(new Declaration(kvp.Key, new List<Card>(kvp.Value)));
            }
        }
    }
}
