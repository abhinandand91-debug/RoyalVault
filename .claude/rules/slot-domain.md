---
paths:
  - "Assets/SlotKit/**"
  - "Assets/RoyalVault/Core/**"
  - "Assets/RoyalVault/Unity/Game/**"
  - "Assets/RoyalVault/Unity/Data/MachineDefinitionAsset.cs"
  - "Assets/RoyalVault/Data/**"
  - "Assets/RoyalVault/Tests/**"
---

# Slot Rules, Randomness and Credits

## Outcomes

- `SpinEngine` picks the stops before anything moves; reels, animation and sound only show that result.
- Animation length, frame rate, input timing or UI state must never change an outcome.
- Never reroll, discard or alter an outcome to improve the presentation or reach a payout.
- The showcase and forced stops are testing aids only; they must never run in a normal build of the game.

## Spin flow

- `SpinFlow` owns the round's states. Transitions are explicit and invalid ones are rejected.
- Ignore duplicate spin requests while a round cannot be interrupted.
- A round is paid exactly once, even if it is replayed, restored or interrupted.
- Define what happens on app pause, scene unload and interrupted presentation.

## Randomness

- All randomness in the rules goes through `IRandom`; do not call `UnityEngine.Random` from SlotKit.
- Tests and simulations use a seeded `SeededRandom`.
- A client-side random source is not tamper-proof; never claim it is.

## Credits and pays

- Credits are whole numbers (`long`); never use floating point for money.
- Check bets, limits, balance and overflow.
- Keep display formatting (`NumberFormat`) separate from values.
- Never change reel strips, pay tables, paylines or feature rules as part of a visual task.
- Any change to the maths must keep the RTP tests in their target band.

## Real money

Real-money play needs a server-authoritative design, legal review per market, secure payments and audit records before any code is written. Never imply that local balances or outcomes are authoritative.
