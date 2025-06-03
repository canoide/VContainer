using System.Collections.Generic;
using UnityEngine;

namespace VContainer.Unity
{
    public static class LifetimeScopeRegistry
    {
        private static readonly Dictionary<LifetimeScopeTag, LifetimeScope> taggedScopes = new Dictionary<LifetimeScopeTag, LifetimeScope>();

        public static void Register(LifetimeScopeTag tag, LifetimeScope scope)
        {
            if (tag == null)
            {
                Debug.LogError("[LifetimeScopeRegistry] Cannot register with a null tag.");
                return;
            }

            if (scope == null)
            {
                Debug.LogError($"[LifetimeScopeRegistry] Cannot register a null scope for tag '{tag.name}'.");
                if (taggedScopes.ContainsKey(tag))
                {
                    Debug.LogWarning($"[LifetimeScopeRegistry] Unregistering previously registered scope for tag '{tag.name}' due to null scope registration.");
                    taggedScopes.Remove(tag);
                }
                return;
            }

            if (taggedScopes.TryGetValue(tag, out var existingScope))
            {
                if (existingScope == scope) // Same scope re-registering, do nothing.
                    return;

                Debug.LogWarning($"[LifetimeScopeRegistry] Overwriting registration for tag '{tag.name}'. " +
                                 $"Previous scope: {existingScope.name}, New scope: {scope.name}");
                taggedScopes[tag] = scope;
            }
            else
            {
                taggedScopes.Add(tag, scope);
            }
            if (VContainerSettings.DiagnosticsEnabled)
            {
                 Debug.Log($"[LifetimeScopeRegistry] Registered scope '{scope.name}' with tag '{tag.name}'.");
            }
        }

        public static void Unregister(LifetimeScopeTag tag, LifetimeScope scope)
        {
            if (tag == null)
            {
                Debug.LogError("[LifetimeScopeRegistry] Cannot unregister with a null tag.");
                return;
            }
             if (scope == null)
            {
                Debug.LogError($"[LifetimeScopeRegistry] Cannot unregister a null scope for tag '{tag.name}'.");
                return;
            }

            if (taggedScopes.TryGetValue(tag, out var registeredScope) && registeredScope == scope)
            {
                taggedScopes.Remove(tag);
                if (VContainerSettings.DiagnosticsEnabled)
                {
                    Debug.Log($"[LifetimeScopeRegistry] Unregistered scope '{scope.name}' with tag '{tag.name}'.");
                }
            }
            else if (registeredScope != null && registeredScope != scope)
            {
                 Debug.LogWarning($"[LifetimeScopeRegistry] Did not unregister scope '{scope.name}' for tag '{tag.name}' "+
                                  $"because a different scope ('{registeredScope.name}') is currently registered with this tag.");
            }
        }

        public static LifetimeScope GetScope(LifetimeScopeTag tag)
        {
            if (tag == null)
            {
                // Debug.LogError("[LifetimeScopeRegistry] Cannot get scope with a null tag."); // Decided against logging error here for flexibility.
                return null;
            }

            if (taggedScopes.TryGetValue(tag, out var scope))
            {
                return scope;
            }
            return null;
        }

        public static IObjectResolver GetContainer(LifetimeScopeTag tag)
        {
            var scope = GetScope(tag);
            return scope?.Container;
        }
    }
}
