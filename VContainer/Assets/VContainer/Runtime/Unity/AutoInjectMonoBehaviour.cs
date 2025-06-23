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
                    Debug.Log($"[AutoInjectMonoBehaviour] {this.gameObject.name} (ID: {this.gameObject.GetInstanceID()}) already processed by {this.GetType().Name}. Skipping auto-injection attempt.");
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
                    Debug.LogWarning($"[AutoInjectMonoBehaviour] {this.gameObject.name} (ID: {this.gameObject.GetInstanceID()}) in class {this.GetType().Name}: Specified TargetScopeTag '{TargetScopeTag.name}' did not find a registered LifetimeScope. Falling back to parent/root scope search.");
                }
            }

            // Fallback or if TargetScopeTag was not used/found
            if (resolver == null)
            {
                var parentScope = GetComponentInParent<LifetimeScope>(true);
                if (parentScope != null && parentScope.Container != null)
                {
                    resolver = parentScope.Container;
                    injectionSource = $"parent LifetimeScope '{parentScope.name}' (ID: {parentScope.gameObject.GetInstanceID()})";
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
                            Debug.LogWarning($"[AutoInjectMonoBehaviour] {this.gameObject.name} (ID: {this.gameObject.GetInstanceID()}) in class {this.GetType().Name}: Error trying to get root LifetimeScope. VContainerSettings might not be fully initialized or RootLifetimeScope prefab is missing/invalid. Error: {ex.Message}");
                        }
                    }

                    if (rootScope != null && rootScope.Container != null)
                    {
                        resolver = rootScope.Container;
                        injectionSource = $"ROOT LifetimeScope '{rootScope.name}' (ID: {rootScope.gameObject.GetInstanceID()})";
                    }
                }
            }

            if (resolver != null)
            {
                resolver.InjectGameObject(this.gameObject);
                _isInjected = true; // Mark as injected AFTER successful injection.
                if (enableDiagnostics)
                    Debug.Log($"[AutoInjectMonoBehaviour] Injected {this.gameObject.name} (ID: {this.gameObject.GetInstanceID()}) in class {this.GetType().Name} using {injectionSource}.");
            }
            else
            {
                // Mark as processed even if failed, to prevent re-attempts by this Awake on this instance.
                _isInjected = true;
                Debug.LogWarning($"[AutoInjectMonoBehaviour] Injection FAILED for {this.gameObject.name} (ID: {this.gameObject.GetInstanceID()}) in class {this.GetType().Name}. No suitable LifetimeScope found or container not ready.");
            }
        }
    }
}
