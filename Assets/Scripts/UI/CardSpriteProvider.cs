using Blot.Cards;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Blot.UI
{
    /// <summary>
    /// Maps a <see cref="Card"/> to the sprite stored under Assets/Art/Textures/Cards/.
    /// Naming convention: {SuitPrefix}_{RankSuffix}.png  (e.g. Club_7.png, Heart_A.png)
    /// Editor-only implementation — sufficient for the development/test scene.
    /// </summary>
    public static class CardSpriteProvider
    {
        private const string BasePath    = "Assets/Art/Textures/Cards/";
        private const string BackPath    = "Assets/Art/Textures/Cards/BackCard1.png";

        public static Sprite GetSprite(Card card)
        {
#if UNITY_EDITOR
            string path = $"{BasePath}{SuitPrefix(card.Suit)}_{RankSuffix(card.Rank)}.png";
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
#else
            return null;
#endif
        }

        public static Sprite GetCardBackSprite()
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<Sprite>(BackPath);
#else
            return null;
#endif
        }

        // ------------------------------------------------------------------ mapping

        private static string SuitPrefix(Suit suit) => suit switch
        {
            Suit.Clubs    => "Club",
            Suit.Diamonds => "Diamond",
            Suit.Hearts   => "Heart",
            Suit.Spades   => "Spade",
            _             => "Club"
        };

        private static string RankSuffix(Rank rank) => rank switch
        {
            Rank.Seven => "7",
            Rank.Eight => "8",
            Rank.Nine  => "9",
            Rank.Ten   => "10",
            Rank.Jack  => "J",
            Rank.Queen => "Q",
            Rank.King  => "K",
            Rank.Ace   => "A",
            _          => "7"
        };
    }
}
