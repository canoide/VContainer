using UnityEngine;
using VContainer; // Required for IObjectResolver
using VContainer.Unity; // Required for LifetimeScope and VContainerSettings

namespace VContainer.Unity
{
    [DefaultExecutionOrder(-4900)]
    [Tooltip("Automatically injects dependencies into this GameObject and its children using VContainer upon Awake. Finds a parent LifetimeScope or falls back to the root scope.")]
    public class AutoInjectComponent : MonoBehaviour
    {
        public static bool WasSkipLogicHitLast = false; // Static flag for testing
        private bool _isInjected = false;

        void Awake()
        {
            if (_isInjected)
            {
                Debug.Log($"AutoInject: {this.gameObject.name} (ID: {this.gameObject.GetInstanceID()}) already processed by {this.GetType().Name}. Skipping auto-injection attempt.");
                WasSkipLogicHitLast = true; // Set the flag when skip logic is hit
                return;
            }

            var parentScope = GetComponentInParent<LifetimeScope>(true);

            if (parentScope != null && parentScope.Container != null)
            {
                parentScope.Container.InjectGameObject(this.gameObject);
                _isInjected = true;
                Debug.Log($"AutoInject: Injected {this.gameObject.name} (ID: {this.gameObject.GetInstanceID()}) using parent LifetimeScope {parentScope.name} (ID: {parentScope.gameObject.GetInstanceID()}).");
            }
            else
            {
                LifetimeScope rootScope = null;
                VContainerSettings settings = VContainerSettings.Instance;
                if (settings != null)
                {
                    try
                    {
                        rootScope = settings.GetOrCreateRootLifetimeScopeInstance();
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"AutoInject: Error trying to get root LifetimeScope for {this.gameObject.name} (ID: {this.gameObject.GetInstanceID()}). VContainerSettings might not be fully initialized or RootLifetimeScope prefab is missing/invalid. Error: {ex.Message}");
                    }
                }

                if (rootScope != null && rootScope.Container != null)
                {
                    rootScope.Container.InjectGameObject(this.gameObject);
                    _isInjected = true;
                    Debug.Log($"AutoInject: Injected {this.gameObject.name} (ID: {this.gameObject.GetInstanceID()}) using ROOT LifetimeScope {rootScope.name} (ID: {rootScope.gameObject.GetInstanceID()}).");
                }
                else
                {
                    Debug.LogWarning($"AutoInject: Injection FAILED for {this.gameObject.name} (ID: {this.gameObject.GetInstanceID()}). No suitable LifetimeScope found or container not ready.");
                    _isInjected = true; // Mark as processed to prevent re-attempts by this Awake.
                }
            }
        }
    }
}
