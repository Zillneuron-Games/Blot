using System;
using Blot.Bidding;
using Blot.Cards;
using Blot.Gameplay.Rules;
using Blot.Players;
using Blot.Scoring;

namespace Blot.Gameplay.Events
{
    /// <summary>
    /// Central static event bus.
    /// Gameplay layer fires; UI (and any other listeners) subscribe.
    /// Always unsubscribe in OnDestroy to prevent ghost listeners.
    /// </summary>
    public static class GameEvents
    {
        // ---- core game flow ------------------------------------------------
        public static event Action                OnCardsDealt;
        public static event Action<Suit>          OnTrumpSelected;
        public static event Action<Player>        OnPlayerTurnStarted;
        public static event Action<Player, Card>  OnCardPlayed;
        public static event Action<Trick>         OnTrickCompleted;
        public static event Action<RoundResult>   OnRoundEnded;
        public static event Action<TeamId>        OnMatchEnded;
        public static event Action                OnGameRestarted;

        // ---- bidding -------------------------------------------------------
        /// <summary>Fired when it is a player's turn to bid. UI shows bidding options.</summary>
        public static event Action<Player>       OnBidRequested;
        /// <summary>A player placed a bid. Bid = null means Pass.</summary>
        public static event Action<Player, Bid>  OnBidPlaced;
        /// <summary>Bidding is over; contract has been decided.</summary>
        public static event Action<Player, Bid>  OnBiddingComplete;
        /// <summary>All players passed — round will be redealt.</summary>
        public static event Action               OnBiddingAllPassed;

        // ---- Belote / Rebelote --------------------------------------------
        public static event Action<Player, BeloteEvent> OnBeloteAnnounced;

        // ---- fire helpers --------------------------------------------------
        public static void CardsDealt()                          => OnCardsDealt?.Invoke();
        public static void TrumpSelected(Suit trump)             => OnTrumpSelected?.Invoke(trump);
        public static void PlayerTurnStarted(Player p)           => OnPlayerTurnStarted?.Invoke(p);
        public static void CardPlayed(Player p, Card c)          => OnCardPlayed?.Invoke(p, c);
        public static void TrickCompleted(Trick t)               => OnTrickCompleted?.Invoke(t);
        public static void RoundEnded(RoundResult r)             => OnRoundEnded?.Invoke(r);
        public static void MatchEnded(TeamId winner)             => OnMatchEnded?.Invoke(winner);
        public static void GameRestarted()                       => OnGameRestarted?.Invoke();

        public static void BidRequested(Player p)                => OnBidRequested?.Invoke(p);
        public static void BidPlaced(Player p, Bid bid)          => OnBidPlaced?.Invoke(p, bid);
        public static void BiddingComplete(Player p, Bid bid)    => OnBiddingComplete?.Invoke(p, bid);
        public static void BiddingAllPassed()                    => OnBiddingAllPassed?.Invoke();
        public static void BeloteAnnounced(Player p, BeloteEvent e) => OnBeloteAnnounced?.Invoke(p, e);
    }
}
