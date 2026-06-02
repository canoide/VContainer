using UnityEngine;
using VContainer; // Required for IObjectResolver
using VContainer.Unity; // Required for LifetimeScope, VContainerSettings, LifetimeScopeTag, LifetimeScopeRegistry

namespace VContainer.Unity
{
    [DefaultExecutionOrder(-4900)] // Keep existing execution order
    [Tooltip("Base class for MonoBehaviours that should automatically have dependencies injected by VContainer upon Awake. Can use a specific LifetimeScopeTag, a parent LifetimeScope, or falls back to the root scope.")]
    public abstract class AutoInjectMonoBehaviour : MonoBehaviour
    {
        [Tooltip("Optional: Specify a LifetimeScopeTag to target a specific scope for injection. If null or the tagged scope is not found, it will try parent scope then root scope.")]
        public LifetimeScopeTag TargetScopeTag = null;

        private bool _isInjected = false;

        protected virtual void Awake()
        {
            VContainerSettings settings = VContainerSettings.Instance;
            var enableDiagnostics = settings.EnableDiagnostics;
            if (_isInjected)
            {
                if (enableDiagnostics)
#if UNITY_6000_4_OR_NEWER
                    Debug.Log($"[AutoInjectMonoBehaviour] {this.gameObject.name} (ID: {this.gameObject.GetEntityId()}) already processed by {this.GetType().Name}. Skipping auto-injection attempt.");
#else
                    Debug.Log($"[AutoInjectMonoBehaviour] {this.gameObject.name} (ID: {this.gameObject.GetInstanceID()}) already processed by {this.GetType().Name}. Skipping auto-injection attempt.");
#endif
                return;
            }

            IObjectResolver resolver = null;
            string injectionSource = "Unknown";

            if (TargetScopeTag != null)
            {
                resolver = LifetimeScopeRegistry.GetContainer(TargetScopeTag);
                if (resolver != null)
                {
                    injectionSource = $"Tagged LifetimeScope with tag '{TargetScopeTag.name}'";
                }
                else
                {
#if UNITY_6000_4_OR_NEWER
                    Debug.LogWarning($"[AutoInjectMonoBehaviour] {this.gameObject.name} (ID: {this.gameObject.GetEntityId()}) in class {this.GetType().Name}: Specified TargetScopeTag '{TargetScopeTag.name}' did not find a registered LifetimeScope. Falling back to parent/root scope search.");
#else
                    Debug.LogWarning($"[AutoInjectMonoBehaviour] {this.gameObject.name} (ID: {this.gameObject.GetInstanceID()}) in class {this.GetType().Name}: Specified TargetScopeTag '{TargetScopeTag.name}' did not find a registered LifetimeScope. Falling back to parent/root scope search.");
#endif
                }
            }

            // Fallback or if TargetScopeTag was not used/found
            if (resolver == null)
            {
                var parentScope = GetComponentInParent<LifetimeScope>(true);
                if (parentScope != null && parentScope.Container != null)
                {
                    resolver = parentScope.Container;
#if UNITY_6000_4_OR_NEWER
                    injectionSource = $"parent LifetimeScope '{parentScope.name}' (ID: {parentScope.gameObject.GetEntityId()})";
#else
                    injectionSource = $"parent LifetimeScope '{parentScope.name}' (ID: {parentScope.gameObject.GetInstanceID()})";
#endif
                }
                else
                {
                    LifetimeScope rootScope = null;
                    if (settings != null)
                    {
                        try
                        {
                            rootScope = settings.GetOrCreateRootLifetimeScopeInstance();
                        }
                        catch (System.Exception ex)
                        {
#if UNITY_6000_4_OR_NEWER
                            Debug.LogWarning($"[AutoInjectMonoBehaviour] {this.gameObject.name} (ID: {this.gameObject.GetEntityId()}) in class {this.GetType().Name}: Error trying to get root LifetimeScope. VContainerSettings might not be fully initialized or RootLifetimeScope prefab is missing/invalid. Error: {ex.Message}");
#else
                            Debug.LogWarning($"[AutoInjectMonoBehaviour] {this.gameObject.name} (ID: {this.gameObject.GetInstanceID()}) in class {this.GetType().Name}: Error trying to get root LifetimeScope. VContainerSettings might not be fully initialized or RootLifetimeScope prefab is missing/invalid. Error: {ex.Message}");
#endif
                        }
                    }

                    if (rootScope != null && rootScope.Container != null)
                    {
                        resolver = rootScope.Container;
#if UNITY_6000_4_OR_NEWER
                        injectionSource = $"ROOT LifetimeScope '{rootScope.name}' (ID: {rootScope.gameObject.GetEntityId()})";
#else
                        injectionSource = $"ROOT LifetimeScope '{rootScope.name}' (ID: {rootScope.gameObject.GetInstanceID()})";
#endif
                    }
                }
            }

            if (resolver != null)
            {
                resolver.InjectGameObject(this.gameObject);
                _isInjected = true; // Mark as injected AFTER successful injection.
                if (enableDiagnostics)
#if UNITY_6000_4_OR_NEWER
                    Debug.Log($"[AutoInjectMonoBehaviour] Injected {this.gameObject.name} (ID: {this.gameObject.GetEntityId()}) in class {this.GetType().Name} using {injectionSource}.");
#else
                    Debug.Log($"[AutoInjectMonoBehaviour] Injected {this.gameObject.name} (ID: {this.gameObject.GetInstanceID()}) in class {this.GetType().Name} using {injectionSource}.");
#endif
            }
            else
            {
                // Mark as processed even if failed, to prevent re-attempts by this Awake on this instance.
                _isInjected = true;
#if UNITY_6000_4_OR_NEWER
                Debug.LogWarning($"[AutoInjectMonoBehaviour] Injection FAILED for {this.gameObject.name} (ID: {this.gameObject.GetEntityId()}) in class {this.GetType().Name}. No suitable LifetimeScope found or container not ready.");
#else
                Debug.LogWarning($"[AutoInjectMonoBehaviour] Injection FAILED for {this.gameObject.name} (ID: {this.gameObject.GetInstanceID()}) in class {this.GetType().Name}. No suitable LifetimeScope found or container not ready.");
#endif
            }
        }
    }
}
