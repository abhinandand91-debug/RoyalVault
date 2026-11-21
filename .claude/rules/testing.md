---
paths:
  - "Assets/**/*.cs"
---

# Testing and Verification

## Where tests live

- EditMode (`Assets/RoyalVault/Tests/EditMode`): SlotKit rules, flow, saving, RTP, coding standards.
- PlayMode (`Assets/RoyalVault/Tests/PlayMode`): intro click-through, showcase wins, reel orders, anticipation, sound cues, and the opt-in screenshot pass.
- Rules that need no scene are tested as plain C# in EditMode.

## What a change should cover

- The expected result, plus invalid and boundary inputs.
- Valid and rejected state transitions.
- Duplicate requests (no double payouts).
- Interrupted lifecycles where relevant (cancel, disable, destroy).
- Save round-trips and old or corrupt save data where relevant.
- A regression test for each fixed bug where practical.

## Discipline

- Tests are deterministic: seeded random sources, no timing guesses.
- Never weaken or delete a test to make a suite pass.
- Never claim code compiles, tests pass or a screen looks right unless that check actually ran.
- When the user has asked to skip testing, still compile, and say clearly which checks did not run.
