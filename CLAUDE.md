# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

"Blot" is a mobile card game (a Belote/Klaberjass-style trick-taking game) built in Unity 6000.4.10f1 (Unity 6), made by Zillneuron. The game is single-player vs. 3 AI opponents (2v2 teams).

## Development environment

This is a Unity project — there is no CLI build/test workflow. Work is done by opening the project in the Unity Editor (6000.4.10f1) or in Rider/Visual Studio via `Blot.slnx`.

- `Assembly-CSharp.csproj` is the main runtime assembly; `Assembly-CSharp-Editor.csproj` is editor-only code (under `Assets/Editor`).
- Play Mode tests (if added) run via Unity's Test Runner (`com.unity.test-framework` modules are present in `Packages/manifest.json`).
- Private package dependencies (`com.zillneuron.uilayout`, `com.zillneuron.utilities`, `com.zillneuron.web`) are pulled from private GitHub repos referenced in `Packages/manifest.json`.

## Architecture

### Core flow: finite state machine

The game loop is driven by `GameStateMachine` (`Assets/Scripts/Core/StateMachine/`), a simple FSM keyed by `GameStateId`. `GameManager` (a MonoBehaviour, `Assets/Scripts/GameManager.cs`) is the scene entry point — it builds all managers, registers every state, wires a shared `GameContext`, and starts the FSM at `GameStateId.GameStart`.

State flow per round:
```
GameStart → DealCards → Bidding → AnnounceDeclarations → RevealDeclarations
  → PlayTrick → EvaluateTrick → (loop PlayTrick/EvaluateTrick for 8 tricks)
  → CheckRoundEnd → RoundEnd → CheckMatchEnd → (loop or → MatchEnd)
```
`SelectTrumpState` is legacy/dead code kept for backward-compat (trump is now chosen via bidding). Each state lives in `Assets/Scripts/Gameplay/States/` and implements `IGameState.Enter/Exit(GameContext)`.

`GameContext` is an immutable bag of references (`MatchManager`, `RoundManager`, `ScoreManager`, `GameStateMachine`, `DeclarationManager`) injected into every state — states read from it but don't own the objects.

### Managers (`Assets/Scripts/Core/Managers/`)

- **MatchManager** — owns the 4 `Player` instances (seat 0 = human, others AI), the deck, shuffling/dealing, and hand validation (8 cards/player, 32 unique cards, no `Suit.NoTrump` leaking into hands).
- **RoundManager** — single source of truth for all turn-order/round state: current trick leader/active player, trick/round indices, trump, bidding contract details (`ContractBidValue`, `ContractTargetPoints`, Kaput contract flags), challenge state ("I Don't Believe" / "I'm Sure"), per-team trick counts, and `BeloteTracker`. Seat order is clockwise: Player0 → 1 → 2 → 3 → 0.
- **ScoreManager** — match-level score totals and the win target (`ScoreManager.WinTarget`).
- **DeclarationManager** — coordinates the declaration announce/reveal flow.

### Event bus

`Blot.Gameplay.Events.GameEvents` (`Assets/Scripts/Gameplay/Events/GameEvents.cs`) is a static C# event bus. Gameplay/state code fires events (`GameEvents.CardPlayed(...)`, `GameEvents.BiddingComplete(...)`, etc.); UI and AI subscribe. UI subscribers must unsubscribe in `OnDestroy` to avoid ghost listeners. This is the only coupling between gameplay logic and presentation — gameplay code never references UI types directly.

### Players (`Assets/Scripts/Players/`)

`Player` is an abstract base class with two implementations: `HumanPlayer` (defers via UI clicks/events) and `AIPlayer`. Each `Player` exposes `RequestPlay`, `RequestBid`, `RequestSureResponse`, `RequestDeclare`, `RequestReveal` — abstract methods that must eventually call the corresponding `Commit*` method to fire an `On*Chosen`/`On*Response` event. AI implementations respond synchronously; human implementations wait for UI input before committing.

### AI (`Assets/Scripts/AI/`)

`AIPlayer` never accesses opponent hands or hidden state — it builds an `AIKnowledgeBase` purely from public `GameEvents` (cards played, bids placed). Bidding decisions go through `AIBiddingAdvisor` + `AIHandEvaluator`; card play goes through `MonteCarloCardPlayAI` (Monte Carlo simulation over the legal card set from `TrickRules`).

### Gameplay rules (`Assets/Scripts/Gameplay/Rules/`)

- `TrickRules` — computes legally playable cards for a player given the current trick, trump, and all players' state (follow-suit/trump rules).
- `BeloteTracker` — tracks Belote/Rebelote (King+Queen of trump) declarations per round.

### Declarations (`Assets/Scripts/Declarations/`)

`DeclarationDetector` finds optimal melds (sequences, etc.) from a hand for a given trump; `DeclarationManager` and the `AnnounceDeclarations`/`RevealDeclarations` states coordinate the announce-then-reveal flow described at the top of `GameStateId`.

### UI (`Assets/Scripts/UI/`)

Pure presentation layer — contains no game logic. `GameUIManager` subscribes to `GameEvents` and updates HUD text/labels. `PlayerHandView`, `CardView`, `TrickAreaView`, etc. render game state; `BiddingUIController`, `DeclarationUIController`, and `NegotiationsDialog` (under `UI/Dialog/`) handle bidding/declaration/challenge interactions for the human player. View fragments live under `UI/View/Fragment/`.

## Conventions

- Namespaces mirror the folder structure under `Assets/Scripts/` (e.g. `Blot.Core.Managers`, `Blot.Gameplay.States`, `Blot.AI`).
- Managers and state classes are plain C# (no MonoBehaviour); only scene/UI entry points (`GameManager`, UI controllers/views) are MonoBehaviours.
- `GameManager.Instance` is the static accessor UI code uses to reach managers and the state machine.
- Debug-only helpers (e.g. forcing a bid via `GameManager`'s `[ContextMenu]` methods) are for testing in the Editor only.
