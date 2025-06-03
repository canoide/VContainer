using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VContainer;
using VContainer.Unity;
using System.Collections;

// 1. Simple injectable service class
public class TestService
{
    public string Message = "Hello from TestService";
}

// 2. MonoBehaviour that consumes the service
public class ServiceConsumer : MonoBehaviour
{
    [Inject]
    public TestService TheService { get; set; }

    public bool WasInjectedDuringAwake { get; private set; }

    void Awake()
    {
        WasInjectedDuringAwake = TheService != null;
        if (WasInjectedDuringAwake) // No VContainerSettings logging here as it's a test utility class
        {
            // Debug.Log($"ServiceConsumer on {gameObject.name} was injected during its Awake.", gameObject);
        }
    }
}

// 3. MonoBehaviour inheriting from AutoInjectMonoBehaviour
public class ServiceConsumerInherited : AutoInjectMonoBehaviour
{
    [Inject]
    public TestService TheService { get; set; }

    public bool WasInjectedUponOwnAwake { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        WasInjectedUponOwnAwake = TheService != null;
        if (WasInjectedUponOwnAwake)
        {
            // Debug.Log($"ServiceConsumerInherited on {gameObject.name} found TheService injected when its own Awake (after base.Awake) ran.", gameObject);
        }
        // else if(!WasInjectedUponOwnAwake && VContainerSettings.Instance != null && VContainerSettings.Instance.EnableErrorLogging)
        // {
        //     Debug.LogWarning($"ServiceConsumerInherited on {gameObject.name} did NOT find TheService injected when its own Awake (after base.Awake) ran.", gameObject);
        // }
    }
}

// Test script
public class AutoInjectionTests
{
    [UnityTest]
    public IEnumerator AutoInjectComponent_InjectsUsingParentScope()
    {
        var parentScopeGo = new GameObject("ParentScopeGO_AIC_Parent");
        var parentScope = parentScopeGo.AddComponent<LifetimeScope>();
        parentScope.Configure(builder => builder.Register<TestService>(Lifetime.Singleton));
        parentScopeGo.SetActive(true);

        var prefabGo = new GameObject("Prefab_AIC_Parent");
        prefabGo.AddComponent<ServiceConsumer>();
        prefabGo.AddComponent<AutoInjectComponent>();
        prefabGo.SetActive(false);

        var instanceGo = UnityEngine.Object.Instantiate(prefabGo, parentScopeGo.transform);
        instanceGo.SetActive(true);
        var instanceConsumer = instanceGo.GetComponent<ServiceConsumer>();

        yield return null;

        Assert.IsNotNull(instanceConsumer.TheService, "AIC_Parent: TheService was not injected.");
        Assert.IsInstanceOf<TestService>(instanceConsumer.TheService, "AIC_Parent: Injected service is not of type TestService.");
        Assert.AreEqual("Hello from TestService", instanceConsumer.TheService.Message, "AIC_Parent: Service message mismatch.");
        Assert.IsTrue(instanceConsumer.WasInjectedDuringAwake, "AIC_Parent: ServiceConsumer.WasInjectedDuringAwake should be true.");

        UnityEngine.Object.Destroy(instanceGo);
        UnityEngine.Object.Destroy(prefabGo);
        UnityEngine.Object.Destroy(parentScopeGo);
    }

    [UnityTest]
    public IEnumerator AutoInjectMonoBehaviour_InjectsUsingParentScope()
    {
        var parentScopeGo = new GameObject("ParentScopeGO_AIMB_Parent");
        var parentScope = parentScopeGo.AddComponent<LifetimeScope>();
        parentScope.Configure(builder => builder.Register<TestService>(Lifetime.Singleton));
        parentScopeGo.SetActive(true);

        var prefabGo = new GameObject("Prefab_AIMB_Parent");
        prefabGo.AddComponent<ServiceConsumerInherited>();
        prefabGo.SetActive(false);

        var instanceGo = UnityEngine.Object.Instantiate(prefabGo, parentScopeGo.transform);
        instanceGo.SetActive(true);
        var instanceConsumer = instanceGo.GetComponent<ServiceConsumerInherited>();

        yield return null;

        Assert.IsNotNull(instanceConsumer.TheService, "AIMB_Parent: TheService was not injected.");
        Assert.IsInstanceOf<TestService>(instanceConsumer.TheService, "AIMB_Parent: Injected service is not of type TestService.");
        Assert.AreEqual("Hello from TestService", instanceConsumer.TheService.Message, "AIMB_Parent: Service message mismatch.");
        Assert.IsTrue(instanceConsumer.WasInjectedUponOwnAwake, "AIMB_Parent: ServiceConsumerInherited.WasInjectedUponOwnAwake should be true.");

        UnityEngine.Object.Destroy(instanceGo);
        UnityEngine.Object.Destroy(prefabGo);
        UnityEngine.Object.Destroy(parentScopeGo);
    }

    [UnityTest]
    public IEnumerator AutoInjectComponent_InjectsUsingRootScopeWhenNoParent()
    {
        LifetimeScope originalRootScope = null;
        GameObject rootScopePrefabGo = null;
        GameObject prefabGo = null;
        GameObject instanceGo = null;
        VContainerSettings settings = null;

        try
        {
            settings = VContainerSettings.Instance;
            if (settings == null) { VContainerSettings.EnsureInstanceExists(); settings = VContainerSettings.Instance; }
            if (settings == null) { Assert.Inconclusive("VContainerSettings.Instance could not be ensured."); yield break; }
            originalRootScope = settings.RootLifetimeScope;

            rootScopePrefabGo = new GameObject("TestRootScopePrefab_AIC_Root");
            var testRootScope = rootScopePrefabGo.AddComponent<LifetimeScope>();
            testRootScope.IsRoot = true;
            testRootScope.Configure(builder => builder.Register<TestService>(Lifetime.Singleton));
            rootScopePrefabGo.SetActive(true);
            yield return null;
            settings.RootLifetimeScope = testRootScope;

            prefabGo = new GameObject("Prefab_AIC_Root");
            prefabGo.AddComponent<ServiceConsumer>();
            prefabGo.AddComponent<AutoInjectComponent>();
            prefabGo.SetActive(false);

            instanceGo = UnityEngine.Object.Instantiate(prefabGo);
            instanceGo.SetActive(true);
            var instanceConsumer = instanceGo.GetComponent<ServiceConsumer>();
            yield return null;

            Assert.IsNotNull(instanceConsumer.TheService, "AIC_Root: TheService was not injected.");
            Assert.IsInstanceOf<TestService>(instanceConsumer.TheService);
            Assert.IsTrue(instanceConsumer.WasInjectedDuringAwake);
        }
        finally
        {
            if (settings != null) settings.RootLifetimeScope = originalRootScope;
            if (instanceGo != null) UnityEngine.Object.Destroy(instanceGo);
            if (prefabGo != null) UnityEngine.Object.Destroy(prefabGo);
            if (rootScopePrefabGo != null) UnityEngine.Object.Destroy(rootScopePrefabGo);
        }
    }

    [UnityTest]
    public IEnumerator AutoInjectMonoBehaviour_InjectsUsingRootScopeWhenNoParent()
    {
        LifetimeScope originalRootScope = null;
        GameObject rootScopePrefabGo = null;
        GameObject prefabGo = null;
        GameObject instanceGo = null;
        VContainerSettings settings = null;
        try
        {
            settings = VContainerSettings.Instance;
            if (settings == null) { VContainerSettings.EnsureInstanceExists(); settings = VContainerSettings.Instance; }
            if (settings == null) { Assert.Inconclusive("VContainerSettings.Instance could not be ensured."); yield break; }
            originalRootScope = settings.RootLifetimeScope;

            rootScopePrefabGo = new GameObject("TestRootScopePrefab_AIMB_Root");
            var testRootScope = rootScopePrefabGo.AddComponent<LifetimeScope>();
            testRootScope.IsRoot = true;
            testRootScope.Configure(builder => builder.Register<TestService>(Lifetime.Singleton));
            rootScopePrefabGo.SetActive(true);
            yield return null;
            settings.RootLifetimeScope = testRootScope;

            prefabGo = new GameObject("Prefab_AIMB_Root");
            prefabGo.AddComponent<ServiceConsumerInherited>();
            prefabGo.SetActive(false);

            instanceGo = UnityEngine.Object.Instantiate(prefabGo);
            instanceGo.SetActive(true);
            var instanceConsumer = instanceGo.GetComponent<ServiceConsumerInherited>();
            yield return null;

            Assert.IsNotNull(instanceConsumer.TheService, "AIMB_Root: TheService was not injected.");
            Assert.IsInstanceOf<TestService>(instanceConsumer.TheService);
            Assert.IsTrue(instanceConsumer.WasInjectedUponOwnAwake);
        }
        finally
        {
            if (settings != null) settings.RootLifetimeScope = originalRootScope;
            if (instanceGo != null) UnityEngine.Object.Destroy(instanceGo);
            if (prefabGo != null) UnityEngine.Object.Destroy(prefabGo);
            if (rootScopePrefabGo != null) UnityEngine.Object.Destroy(rootScopePrefabGo);
        }
    }

    [UnityTest]
    public IEnumerator AutoInjectComponent_DoesNotInjectWhenNoScopeFound()
    {
        LifetimeScope originalRootScope = null;
        GameObject prefabGo = null;
        GameObject instanceGo = null;
        VContainerSettings settings = null;
        try
        {
            settings = VContainerSettings.Instance;
            if (settings == null) { VContainerSettings.EnsureInstanceExists(); settings = VContainerSettings.Instance; }
            if (settings == null) { Assert.Inconclusive("VContainerSettings.Instance could not be ensured."); yield break; }

            originalRootScope = settings.RootLifetimeScope;
            settings.RootLifetimeScope = null;

            prefabGo = new GameObject("Prefab_AIC_NoScope");
            prefabGo.AddComponent<ServiceConsumer>();
            prefabGo.AddComponent<AutoInjectComponent>();
            prefabGo.SetActive(false);

            instanceGo = UnityEngine.Object.Instantiate(prefabGo);
            instanceGo.SetActive(true);
            var instanceConsumer = instanceGo.GetComponent<ServiceConsumer>();
            yield return null;

            Assert.IsNull(instanceConsumer.TheService, "AIC_NoScope: TheService should be null.");
            Assert.IsFalse(instanceConsumer.WasInjectedDuringAwake);
        }
        finally
        {
            if (settings != null) settings.RootLifetimeScope = originalRootScope;
            if (instanceGo != null) UnityEngine.Object.Destroy(instanceGo);
            if (prefabGo != null) UnityEngine.Object.Destroy(prefabGo);
        }
    }

    [UnityTest]
    public IEnumerator AutoInjectMonoBehaviour_DoesNotInjectWhenNoScopeFound()
    {
        LifetimeScope originalRootScope = null;
        GameObject prefabGo = null;
        GameObject instanceGo = null;
        VContainerSettings settings = null;
        try
        {
            settings = VContainerSettings.Instance;
            if (settings == null) { VContainerSettings.EnsureInstanceExists(); settings = VContainerSettings.Instance; }
            if (settings == null) { Assert.Inconclusive("VContainerSettings.Instance could not be ensured."); yield break; }

            originalRootScope = settings.RootLifetimeScope;
            settings.RootLifetimeScope = null;

            prefabGo = new GameObject("Prefab_AIMB_NoScope");
            prefabGo.AddComponent<ServiceConsumerInherited>();
            prefabGo.SetActive(false);

            instanceGo = UnityEngine.Object.Instantiate(prefabGo);
            instanceGo.SetActive(true);
            var instanceConsumer = instanceGo.GetComponent<ServiceConsumerInherited>();
            yield return null;

            Assert.IsNull(instanceConsumer.TheService, "AIMB_NoScope: TheService should be null.");
            Assert.IsFalse(instanceConsumer.WasInjectedUponOwnAwake);
        }
        finally
        {
            if (settings != null) settings.RootLifetimeScope = originalRootScope;
            if (instanceGo != null) UnityEngine.Object.Destroy(instanceGo);
            if (prefabGo != null) UnityEngine.Object.Destroy(prefabGo);
        }
    }

    [UnityTest]
    public IEnumerator AutoInjectComponent_InjectsIntoChildObjects()
    {
        GameObject parentScopeGo = null;
        GameObject rootPrefabGo = null;
        GameObject instanceRootGo = null;

        try
        {
            parentScopeGo = new GameObject("ParentScope_ChildTest");
            var parentScope = parentScopeGo.AddComponent<LifetimeScope>();
            parentScope.Configure(builder => builder.Register<TestService>(Lifetime.Singleton));
            parentScopeGo.SetActive(true);
            yield return null;

            rootPrefabGo = new GameObject("RootPrefab_ChildTest");
            var childPrefabGo = new GameObject("ChildPrefab_ChildTest");
            childPrefabGo.AddComponent<ServiceConsumer>();
            childPrefabGo.transform.SetParent(rootPrefabGo.transform);
            rootPrefabGo.AddComponent<AutoInjectComponent>();
            rootPrefabGo.SetActive(false);

            instanceRootGo = UnityEngine.Object.Instantiate(rootPrefabGo, parentScopeGo.transform);
            instanceRootGo.SetActive(true);
            var childConsumer = instanceRootGo.GetComponentInChildren<ServiceConsumer>();
            yield return null;

            Assert.IsNotNull(childConsumer, "ChildConsumer was not found.");
            Assert.IsNotNull(childConsumer.TheService, "ChildConsumer.TheService was not injected.");
            Assert.IsInstanceOf<TestService>(childConsumer.TheService, "ChildConsumer.TheService is not of type TestService.");
            Assert.AreEqual("Hello from TestService", childConsumer.TheService.Message, "ChildConsumer.TheService message mismatch.");
            Assert.IsTrue(childConsumer.WasInjectedDuringAwake, "ChildConsumer.WasInjectedDuringAwake should be true.");
        }
        finally
        {
            if (instanceRootGo != null) UnityEngine.Object.Destroy(instanceRootGo);
            if (rootPrefabGo != null) UnityEngine.Object.Destroy(rootPrefabGo);
            if (parentScopeGo != null) UnityEngine.Object.Destroy(parentScopeGo);
        }
    }

    [UnityTest]
    public IEnumerator AutoInjectComponent_SkipsIfAlreadyInjectedByVContainerInstantiate()
    {
        GameObject parentScopeGo = null;
        GameObject prefabGo = null;
        GameObject instanceGo = null;
        ServiceConsumer instanceConsumer = null;

        AutoInjectComponent.WasSkipLogicHitLast = false;

        try
        {
            parentScopeGo = new GameObject("ParentScope_SkipTest");
            var parentScope = parentScopeGo.AddComponent<LifetimeScope>();
            parentScope.Configure(builder => builder.Register<TestService>(Lifetime.Singleton));
            parentScopeGo.SetActive(true);
            yield return null;

            prefabGo = new GameObject("Prefab_SkipTest");
            var prefabConsumer = prefabGo.AddComponent<ServiceConsumer>();
            prefabGo.AddComponent<AutoInjectComponent>();
            prefabGo.SetActive(false);

            instanceConsumer = parentScope.Container.Instantiate(prefabConsumer);
            instanceGo = instanceConsumer.gameObject;
            instanceGo.SetActive(true);
            yield return null;

            Assert.IsNotNull(instanceConsumer.TheService, "SkipTest: TheService was not injected by Container.Instantiate.");
            Assert.IsInstanceOf<TestService>(instanceConsumer.TheService, "SkipTest: Injected service is not of type TestService.");
            Assert.AreEqual("Hello from TestService", instanceConsumer.TheService.Message, "SkipTest: Service message mismatch.");
            Assert.IsTrue(instanceConsumer.WasInjectedDuringAwake, "SkipTest: ServiceConsumer.WasInjectedDuringAwake should be true (from Container.Instantiate).");
            Assert.IsTrue(AutoInjectComponent.WasSkipLogicHitLast, "SkipTest: AutoInjectComponent did not hit the skip logic.");
        }
        finally
        {
            AutoInjectComponent.WasSkipLogicHitLast = false;
            if (instanceGo != null) UnityEngine.Object.Destroy(instanceGo);
            if (prefabGo != null) UnityEngine.Object.Destroy(prefabGo);
            if (parentScopeGo != null) UnityEngine.Object.Destroy(parentScopeGo);
        }
    }
}
