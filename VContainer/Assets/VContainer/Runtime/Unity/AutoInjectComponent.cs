using UnityEngine;
using VContainer; // Required for IObjectResolver
using VContainer.Unity; // Required for LifetimeScope, VContainerSettings, LifetimeScopeTag, LifetimeScopeRegistry

namespace VContainer.Unity
{
    [DefaultExecutionOrder(-4900)] // Keep existing execution order
    [Tooltip("Automatically injects dependencies into this GameObject and its children using VContainer upon Awake. Can use a specific LifetimeScopeTag, a parent LifetimeScope, or fall back to the root scope.")]
    public class AutoInjectComponent : MonoBehaviour
    {
        [Tooltip("Optional: Specify a LifetimeScopeTag to target a specific scope for injection. If null or the tagged scope is not found, it will try parent scope then root scope.")]
        public LifetimeScopeTag TargetScopeTag = null;

        public static bool WasSkipLogicHitLast = false; // Static flag for testing
        private bool _isInjected = false;

        void Awake()
        {
            VContainerSettings settings = VContainerSettings.Instance;
            var enableDiagnostics = settings.EnableDiagnostics;
            if (_isInjected)
            {
                if (enableDiagnostics)
#if UNITY_6000_4_OR_NEWER
                    Debug.Log($"[AutoInjectComponent] {this.gameObject.name} (ID: {this.gameObject.GetEntityId()}) already processed. Skipping auto-injection attempt.");
#else
                    Debug.Log($"[AutoInjectComponent] {this.gameObject.name} (ID: {this.gameObject.GetInstanceID()}) already processed. Skipping auto-injection attempt.");
#endif
                WasSkipLogicHitLast = true; // Set the flag when skip logic is hit
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
                    Debug.LogWarning($"[AutoInjectComponent] {this.gameObject.name} (ID: {this.gameObject.GetEntityId()}): Specified TargetScopeTag '{TargetScopeTag.name}' did not find a registered LifetimeScope. Falling back to parent/root scope search.");
#else
                    Debug.LogWarning($"[AutoInjectComponent] {this.gameObject.name} (ID: {this.gameObject.GetInstanceID()}): Specified TargetScopeTag '{TargetScopeTag.name}' did not find a registered LifetimeScope. Falling back to parent/root scope search.");
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
                             Debug.LogWarning($"[AutoInjectComponent] {this.gameObject.name} (ID: {this.gameObject.GetEntityId()}): Error trying to get root LifetimeScope. VContainerSettings might not be fully initialized or RootLifetimeScope prefab is missing/invalid. Error: {ex.Message}");
#else
                             Debug.LogWarning($"[AutoInjectComponent] {this.gameObject.name} (ID: {this.gameObject.GetInstanceID()}): Error trying to get root LifetimeScope. VContainerSettings might not be fully initialized or RootLifetimeScope prefab is missing/invalid. Error: {ex.Message}");
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
                _isInjected = true;
                if (enableDiagnostics)
#if UNITY_6000_4_OR_NEWER
                    Debug.Log($"[AutoInjectComponent] Injected {this.gameObject.name} (ID: {this.gameObject.GetEntityId()}) using {injectionSource}.");
#else
                    Debug.Log($"[AutoInjectComponent] Injected {this.gameObject.name} (ID: {this.gameObject.GetInstanceID()}) using {injectionSource}.");
#endif
            }
            else
            {
                // Mark as processed even if failed, to prevent re-attempts by this Awake on this instance.
                // Other components or manual calls might still attempt injection later if needed.
                _isInjected = true;
#if UNITY_6000_4_OR_NEWER
                Debug.LogWarning($"[AutoInjectComponent] Injection FAILED for {this.gameObject.name} (ID: {this.gameObject.GetEntityId()}). No suitable LifetimeScope found or container not ready.");
#else
                Debug.LogWarning($"[AutoInjectComponent] Injection FAILED for {this.gameObject.name} (ID: {this.gameObject.GetInstanceID()}). No suitable LifetimeScope found or container not ready.");
#endif
            }
        }
    }
}
