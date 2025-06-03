using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace VContainer.Unity
{
    [DefaultExecutionOrder(-4900)]
    [Tooltip("Base class for MonoBehaviours that should automatically have dependencies injected by VContainer upon Awake. Finds a parent LifetimeScope or falls back to the root scope.")]
    public abstract class AutoInjectMonoBehaviour : MonoBehaviour
    {
        private bool _isInjected = false;

        protected virtual void Awake()
        {
            if (_isInjected)
            {
                Debug.Log($"AutoInject: {this.gameObject.name} (ID: {this.gameObject.GetInstanceID()}) already processed by {this.GetType().Name}. Skipping auto-injection attempt.");
                return;
            }
            _isInjected = true;

            var parentScope = GetComponentInParent<LifetimeScope>(true);

            if (parentScope != null && parentScope.Container != null)
            {
                parentScope.Container.InjectGameObject(this.gameObject);
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
                    Debug.Log($"AutoInject: Injected {this.gameObject.name} (ID: {this.gameObject.GetInstanceID()}) using ROOT LifetimeScope {rootScope.name} (ID: {rootScope.gameObject.GetInstanceID()}).");
                }
                else
                {
                    Debug.LogWarning($"AutoInject: Injection FAILED for {this.gameObject.name} (ID: {this.gameObject.GetInstanceID()}). No suitable LifetimeScope found or container not ready.");
                }
            }
        }
    }
}
