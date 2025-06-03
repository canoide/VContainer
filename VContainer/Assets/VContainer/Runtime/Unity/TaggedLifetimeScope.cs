using UnityEngine;

namespace VContainer.Unity
{
    [DefaultExecutionOrder(-4950)] // Slightly after LifetimeScope's default (-5000) to ensure base Awake runs first.
    public class TaggedLifetimeScope : LifetimeScope
    {
        [SerializeField]
        private LifetimeScopeTag scopeTag;

        public LifetimeScopeTag ScopeTag => scopeTag;

        protected override void Awake()
        {
            base.Awake(); // Ensure LifetimeScope.Awake (which might call Build) runs first.
            if (scopeTag != null)
            {
                LifetimeScopeRegistry.Register(scopeTag, this);
            }
            else
            {
                if (VContainerSettings.DiagnosticsEnabled)
                {
                    Debug.Log($"[TaggedLifetimeScope] Scope '{this.name}' does not have a ScopeTag assigned.", this);
                }
            }
        }

        protected override void OnDestroy()
        {
            // Unregister before the container is disposed by base.OnDestroy()
            if (scopeTag != null)
            {
                LifetimeScopeRegistry.Unregister(scopeTag, this);
            }
            base.OnDestroy();
        }
    }
}
