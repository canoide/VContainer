# VContainer AutoInject Documentation

`com.toucanstudio.vcontainer-autoinject` provides automatic dependency injection helpers for [VContainer](https://github.com/hadashiA/VContainer) when GameObjects and prefabs are instantiated using Unity's native `UnityEngine.Object.Instantiate()`, object pools, or third-party spawning systems.

---

## 1. Overview & Architecture

### The Problem
Standard VContainer promotes the use of `IObjectResolver.Instantiate()` or manual `resolver.InjectGameObject(go)` after spawning. However, many Unity architectures rely on standard `Object.Instantiate()` (e.g., Unity UI prefabs, network spawners, visual effect pools, procedural generation).

### The Solution
This package provides two primary utilities to inject dependencies automatically during Unity's `Awake()` lifecycle:
- **`AutoInjectComponent`**: A standalone MonoBehaviour you can attach to any GameObject/Prefab. It injects dependencies into the GameObject and all child components.
- **`AutoInjectMonoBehaviour`**: An abstract base class for scripts that prefer inheritance-based auto-injection.
- **`TaggedLifetimeScope` & `LifetimeScopeTag`**: Allows scoping dependencies by tag without requiring a parent-child transform hierarchy.

### Execution Order Pipeline
```
-5000  LifetimeScope.Awake()           (Initializes container)
-4950  TaggedLifetimeScope.Awake()     (Registers itself in LifetimeScopeRegistry)
-4900  AutoInjectComponent.Awake()     (Resolves scope & calls InjectGameObject)
-4900  AutoInjectMonoBehaviour.Awake() (Resolves scope & calls InjectGameObject)
    0  User Scripts Awake() / Start()  (Dependencies are already injected & available!)
```

---

## 2. API Reference

### Namespace
```csharp
using ToucanStudio.VContainer.AutoInject;
using VContainer;
using VContainer.Unity;
```

### Components

#### `AutoInjectComponent`
- **Class**: `public class AutoInjectComponent : MonoBehaviour`
- **Execution Order**: `[DefaultExecutionOrder(-4900)]`
- **Fields**:
  - `public LifetimeScopeTag TargetScopeTag`: Optional tag reference to target a specific `TaggedLifetimeScope`.
- **Resolution Strategy**:
  1. If `TargetScopeTag != null`, looks up container in `LifetimeScopeRegistry`.
  2. If not found or null, searches up the hierarchy: `GetComponentInParent<LifetimeScope>(true)`.
  3. If no parent scope exists, falls back to the Root `LifetimeScope` via `VContainerSettings.Instance.GetOrCreateRootLifetimeScopeInstance()`.
- **Features**:
  - Injects entire hierarchy recursively (`resolver.InjectGameObject(this.gameObject)`).
  - Skips redundant injection if the object was already injected by `IObjectResolver.Instantiate()`.

#### `AutoInjectMonoBehaviour`
- **Class**: `public abstract class AutoInjectMonoBehaviour : MonoBehaviour`
- **Execution Order**: `[DefaultExecutionOrder(-4900)]`
- **Fields**:
  - `public LifetimeScopeTag TargetScopeTag`: Optional tag reference.
- **Lifecycle**:
  - Override `protected virtual void Awake()` if needed, but you **MUST** call `base.Awake()` first to trigger injection.

#### `LifetimeScopeTag`
- **Class**: `public class LifetimeScopeTag : ScriptableObject`
- **Creation**: Right-click in Project view -> `Create -> VContainer -> Lifetime Scope Tag`.
- Serves as an asset identifier to link spawnees with a specific lifetime scope.

#### `TaggedLifetimeScope`
- **Class**: `public class TaggedLifetimeScope : LifetimeScope`
- **Execution Order**: `[DefaultExecutionOrder(-4950)]`
- **Fields**:
  - `[SerializeField] private LifetimeScopeTag scopeTag`: The tag asset identifying this scope.
- Automatically registers to `LifetimeScopeRegistry` in `Awake()` and unregisters in `OnDestroy()`.

#### `LifetimeScopeRegistry`
- **Class**: `public static class LifetimeScopeRegistry`
- **Methods**:
  - `Register(LifetimeScopeTag tag, LifetimeScope scope)`
  - `Unregister(LifetimeScopeTag tag, LifetimeScope scope)`
  - `GetScope(LifetimeScopeTag tag)` -> `LifetimeScope`
  - `GetContainer(LifetimeScopeTag tag)` -> `IObjectResolver`

---

## 3. How to Use (Instructions for Developers & AI)

### Scenario A: Add auto-injection to an existing Prefab (Recommended)
1. Add your consumer component with `[Inject]` to the prefab.
2. Add `AutoInjectComponent` to the root GameObject of the prefab.
3. Spawn the prefab anywhere with `UnityEngine.Object.Instantiate(prefab)`.

```csharp
using UnityEngine;
using VContainer;

public class EnemyHealthDisplay : MonoBehaviour
{
    [Inject]
    private IAudioService _audioService; // Will be injected before Start()

    void Start()
    {
        _audioService?.PlaySound("spawn");
    }
}
```

### Scenario B: Inherit from `AutoInjectMonoBehaviour`
```csharp
using UnityEngine;
using VContainer;
using ToucanStudio.VContainer.AutoInject;

public class PlayerHUD : AutoInjectMonoBehaviour
{
    [Inject]
    private IPlayerState _playerState;

    protected override void Awake()
    {
        base.Awake(); // CRITICAL: base.Awake() performs the injection
        Debug.Log("Injected! State: " + _playerState);
    }
}
```

### Scenario C: Tagged Scopes (Cross-Scene or Non-Hierarchical)
When a prefab is instantiated outside of the parent transform hierarchy of a `LifetimeScope`, use a `LifetimeScopeTag`:
1. Create a `LifetimeScopeTag` asset (e.g. `BattleScopeTag.asset`).
2. Attach `TaggedLifetimeScope` to your scene scope GameObject, configure your registrations, and assign `BattleScopeTag`.
3. In your prefab's `AutoInjectComponent` (or `AutoInjectMonoBehaviour`), assign `BattleScopeTag` to `TargetScopeTag`.
4. When instantiated anywhere in the scene, the prefab will resolve dependencies from that tagged scope.

---

## 4. Diagnostics & Logging
Enable diagnostics in `VContainerSettings` (`EnableDiagnostics = true`).
Logs include:
- `[AutoInjectComponent] Injected <GameObject> using <ScopeSource>.`
- `[AutoInjectComponent] <GameObject> already processed. Skipping auto-injection attempt.`
- Supports Unity 6000.4+ `GetEntityId()` and legacy `GetInstanceID()`.
