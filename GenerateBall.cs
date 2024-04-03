using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class GenerateBall : SingletonComponent<GenerateBall>
{
    private readonly Dictionary<AssetReference, List<GameObject>> _spawnedParticleSystems = new();

    /// The Queue holds requests to spawn an instanced that were made while we are already loading the asset
    /// They are spawned once the addressable is loaded, in the order requested
    private readonly Dictionary<AssetReference, Queue<BallGenerateData>> _queuedSpawnRequests = new();

    private readonly Dictionary<AssetReference, AsyncOperationHandle<GameObject>> _asyncOperationHandles = new();

    internal void Generate_Ball(AssetReference assetReference, BallGenerateData data)
    {
        if (!assetReference.RuntimeKeyIsValid()) return;

        if (_asyncOperationHandles.ContainsKey(assetReference))
        {
            if (_asyncOperationHandles[assetReference].IsDone) SpawnFromLoadedReference(assetReference, data);
            else EnqueueSpawnForAfterInitialization(assetReference, data);
            return;
        }

        LoadAndSpawn(assetReference, data);
    }

    private void LoadAndSpawn(AssetReference assetReference, BallGenerateData data)
    {
        var op = Addressables.LoadAssetAsync<GameObject>(assetReference);
        _asyncOperationHandles[assetReference] = op;
        op.Completed += _ =>
        {
            SpawnFromLoadedReference(assetReference, data);
            if (!_queuedSpawnRequests.ContainsKey(assetReference)) return;
            while (_queuedSpawnRequests[assetReference]?.Any() == true)
            {
                var position = _queuedSpawnRequests[assetReference].Dequeue();
                SpawnFromLoadedReference(assetReference, position);
            }
        };
    }

    private void EnqueueSpawnForAfterInitialization(AssetReference assetReference, BallGenerateData data)
    {
        if (_queuedSpawnRequests.ContainsKey(assetReference) == false)
            _queuedSpawnRequests[assetReference] = new Queue<BallGenerateData>();
        _queuedSpawnRequests[assetReference].Enqueue(data);
    }

    private void SpawnFromLoadedReference(AssetReference assetReference, BallGenerateData data)
    {
        assetReference.InstantiateAsync(data.position, Quaternion.identity, data.parent).Completed += prefabBall =>
        {
            if (_spawnedParticleSystems.ContainsKey(assetReference) == false)
            {
                _spawnedParticleSystems[assetReference] = new List<GameObject>();
            }

            prefabBall.Result.transform.SetParent(data.parent);
            prefabBall.Result.transform.position = data.position;
            prefabBall.Result.transform.localScale = data.scale;
            prefabBall.Result.name = data.name;

            GameManager.Inst.allObjectsGrid.Add(prefabBall.Result.transform);

            if (prefabBall.Result.GetComponent<GemController>() != null)
                prefabBall.Result.GetComponent<GemController>().Set_Ball_Color(data.isColorEnable, data.ballColor);

            _spawnedParticleSystems[assetReference].Add(prefabBall.Result);
            var notify = prefabBall.Result.AddComponent<NotifyOnDestroy>();
            notify.Destroyed += Remove;
            notify.AssetReference = assetReference;
        };
    }

    private void Remove(AssetReference assetReference, NotifyOnDestroy obj)
    {
        Addressables.ReleaseInstance(obj.gameObject);
        _spawnedParticleSystems[assetReference].Remove(obj.gameObject);
        if (_spawnedParticleSystems[assetReference].Count != 0) return;
        if (_asyncOperationHandles[assetReference].IsValid())
            Addressables.Release(_asyncOperationHandles[assetReference]);
        _asyncOperationHandles.Remove(assetReference);
    }

    public bool CheckAllLoaded()
    {
        return _asyncOperationHandles.All(x => x.Value.IsDone);
    }
}

public class BallGenerateData
{
    public Transform parent;
    public Vector3 position;
    public Vector3 scale;
    public Color ballColor;
    public bool isColorEnable;
    public string name;
}