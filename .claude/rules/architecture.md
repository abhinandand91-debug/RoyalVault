# Architecture

## Boundaries in this project

- `SlotKit.Core` decides everything: outcomes, pays, features, session state. It is plain C# with no Unity references (`noEngineReferences`).
- `GameKit` holds UI and animation helpers any game can use. It never references slots or Royal Vault.
- `RoyalVault.Unity` presents what SlotKit decided. It never decides an outcome, a payout or a feature trigger.
- `GameBootstrap` is the composition root. Wire new systems there with explicit references, not scene searches.
- Dependencies point inward: Royal Vault uses GameKit and SlotKit; the kits never reference Royal Vault.

## Components and state

- A `MonoBehaviour` adapts Unity to the game; it is not the whole feature. Keep lifecycle methods small.
- ScriptableObjects hold authored data (machines, theme, story, sounds). Never write per-player runtime state into an asset.
- Runtime state lives in plain C# objects with a clear owner (`GameSession`, `SpinFlow`).
- No service locators, singletons or global event buses.

## Events

- Use C# events for one-to-many notifications and direct calls for one-to-one.
- Name events after facts (`SpinFinished`, `ReelStopped`) and commands after requests (`RequestSpin`).
- Every subscription has an owner that unsubscribes when its lifetime ends.

## Saved data

- Saved data carries a version number; changing its shape needs a migration from older versions.
- Treat loaded data as untrusted: check the version, ranges and missing fields, and fall back safely on corrupt data.
- PlayerPrefs is for small, non-sensitive data only.

## Before adding a layer, pattern, package or assembly

1. What concrete change or test becomes easier?
2. Does the project already have a way to do it?
3. What does it cost at run time and to maintain?
4. Would a plain C# class or simple composition do?

If the benefit is not clear, do not add it.
