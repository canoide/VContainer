# VContainer AutoInject (`com.toucanstudio.vcontainer-autoinject`)

[![Unity](https://img.shields.io/badge/Unity-2021.3%2B-blue.svg)](https://unity.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

**VContainer AutoInject** is an extension package for [VContainer](https://github.com/hadashiA/VContainer) that enables automatic dependency injection for prefabs and GameObjects spawned using `UnityEngine.Object.Instantiate()`, object pools, or custom spawners.

---

## 📌 Why Use This Package?

In standard VContainer:
- Dependency injection during instantiation typically requires `IObjectResolver.Instantiate()`.
- If an object is created via standard `UnityEngine.Object.Instantiate()` or a 3rd party asset (e.g., UI systems, particle managers, networked spawners), dependencies are not automatically injected unless you manually invoke `resolver.InjectGameObject(instance)`.

**VContainer AutoInject solves this seamlessly** by performing injection during Unity's `Awake()` lifecycle phase with strict execution order guarantees.

---

## 🚀 Key Features

- **`AutoInjectComponent`**: Drop onto any prefab or GameObject. Recursively injects dependencies across the GameObject and all children upon `Awake()`.
- **`AutoInjectMonoBehaviour`**: Base class for MonoBehaviours wanting automatic self-injection.
- **`TaggedLifetimeScope` & `LifetimeScopeTag`**: Tag lifetime scopes with ScriptableObject assets so instantiated prefabs can find their target container anywhere in the scene without needing to be parented to the scope GameObject.
- **Smart Execution Order**: Runs at `-4900` (after `LifetimeScope` `-5000` and `TaggedLifetimeScope` `-4950`, but before standard user scripts at `0`).
- **Redundant Injection Protection**: Automatically skips injection if the object was already injected via `IObjectResolver.Instantiate()`.
- **Unity 6+ Ready**: Fully compatible with Unity 6000.4+ (`GetEntityId()`) and previous versions (`GetInstanceID()`).

---

## 📦 Installation

### As an Embedded Package in this repository
The package resides in `VContainer/Packages/com.toucanstudio.vcontainer-autoinject` and is automatically picked up by Unity.

### In other Unity projects via Git URL
Add the package to your `Packages/manifest.json`:
```json
{
  "dependencies": {
    "jp.hadashikick.vcontainer": "1.18.0",
    "com.toucanstudio.vcontainer-autoinject": "https://github.com/canoide/VContainer.git?path=VContainer/Packages/com.toucanstudio.vcontainer-autoinject"
  }
}
```

---

## 🛠️ Usage Guide

### 1. Using `AutoInjectComponent` (Recommended)

1. Create your MonoBehaviour with `[Inject]` dependencies:
   ```csharp
   using UnityEngine;
   using VContainer;

   public class EnemyController : MonoBehaviour
   {
       [Inject]
       private IAudioService _audio;

       [Inject]
       private IGameSettings _settings;

       void Start()
       {
           _audio.PlaySound("EnemySpawn");
       }
   }
   ```
2. In the Unity Inspector, attach `AutoInjectComponent` to your Prefab's root GameObject.
3. Spawn your prefab anywhere using standard Unity code:
   ```csharp
   GameObject enemy = Instantiate(enemyPrefab);
   // Injected automatically before Start()!
   ```

### 2. Using `AutoInjectMonoBehaviour` (Inheritance)

```csharp
using UnityEngine;
using VContainer;
using ToucanStudio.VContainer.AutoInject;

public class ScorePopup : AutoInjectMonoBehaviour
{
    [Inject]
    private IScoreManager _scoreManager;

    protected override void Awake()
    {
        base.Awake(); // CRITICAL: base.Awake() runs the injection logic!
        Debug.Log("Current score: " + _scoreManager.CurrentScore);
    }
}
```

### 3. Tagged LifetimeScopes

When prefabs are spawned outside a scope's transform hierarchy:
1. Create a tag asset: Right-click in Project -> `Create -> VContainer -> Lifetime Scope Tag` (e.g. `GameScopeTag.asset`).
2. Replace or inherit your scene scope with `TaggedLifetimeScope` and assign `GameScopeTag`.
3. In the prefab's `AutoInjectComponent` (or `AutoInjectMonoBehaviour`), assign `GameScopeTag` to `TargetScopeTag`.
4. The spawned object will immediately locate and resolve from `GameScopeTag`!

---

## 🧠 Instructions for AI Coding Assistants

When writing code that uses this package:
1. **Namespace**: Always include `using ToucanStudio.VContainer.AutoInject;`.
2. **Inheritance Rule**: If creating a class inheriting from `AutoInjectMonoBehaviour` and overriding `Awake()`, **always** include `base.Awake();` before accessing injected fields.
3. **Execution Order**: Do not assign custom `[DefaultExecutionOrder]` less than `-4900` on scripts that consume auto-injected dependencies, because auto-injection runs at `-4900`.
4. **Prefer Component over Inheritance**: When generating new prefabs or components, prefer adding `AutoInjectComponent` to the GameObject rather than forcing base-class inheritance, keeping scripts free to inherit from other custom base classes.

---

## 📄 License

This package is licensed under the MIT License. See [LICENSE](LICENSE) for details.
