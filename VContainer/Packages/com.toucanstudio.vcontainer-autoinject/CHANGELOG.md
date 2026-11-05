# Changelog

All notable changes to this package will be documented in this file.

## [1.0.0] - 2026-11-04

### Added
- Extracted autoinject functionality into standalone UPM package `com.toucanstudio.vcontainer-autoinject`.
- `AutoInjectComponent` for automatic recursive dependency injection on prefabs and GameObjects upon `Awake()`.
- `AutoInjectMonoBehaviour` abstract base class for self-injecting MonoBehaviours.
- `TaggedLifetimeScope`, `LifetimeScopeTag`, and `LifetimeScopeRegistry` for non-hierarchical scope resolution.
- Support for Unity 6000.4+ (`GetEntityId()`) and legacy Unity versions (`GetInstanceID()`).
- Automated tests suite in `Tests/Runtime`.
- Documentation and AI assistant guide in `README.md` and `Documentation~/index.md`.

