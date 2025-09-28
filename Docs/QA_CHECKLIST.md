# Golden Wrap — QA Checklist (Sprint 10)

This checklist covers the acceptance tests required for the Sprint 10 delivery.

## Performance & Stability
- [ ] WebGL build runs at a stable 60 FPS on reference hardware (i5-8400 + integrated GPU) with no GC spikes above 5 ms.
- [ ] Memory footprint in WebGL player stays below 384 MB during 30-minute soak test.
- [ ] Object pools show no unexpected instantiation spikes after warm-up (verify candy count stays constant).

## Gameplay Integrity
- [ ] Candy spawn/despawn remains deterministic after prewarming pools and no duplicate candies appear.
- [ ] Bliss activation, combo progression, and contract tracking remain unaffected by pooling changes.
- [ ] Force reset (e.g., returning to menu or restarting) clears all active candies and restores pools.

## Build Settings
- [ ] WebGL player uses IL2CPP, Brotli compression, WebAssembly linker, and 384 MB memory cap.
- [ ] Managed stripping level = Low, engine code stripping enabled, incremental GC on.
- [ ] Target frame rate locked to 60 FPS with VSync disabled.

## Regression Sweep
- [ ] Tutorial, Contracts, Upgrades, and Multi-line modes play without new blocking issues.
- [ ] Audio layers and localisation load correctly in WebGL build.
- [ ] Settings persist after refresh (save/load sanity).

Mark each item after verifying on a build candidate.
