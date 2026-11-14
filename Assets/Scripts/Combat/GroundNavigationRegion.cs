using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SpaceRunner.Combat
{
    // Builds navigation from the level's saved colliders; authored levels may supply a baked NavMesh instead.
    public sealed class GroundNavigationRegion : MonoBehaviour
    {
        [SerializeField] private Vector3 center = new(0, 2, 6);
        [SerializeField] private Vector3 size = new(20, 10, 34);
        [SerializeField] private LayerMask geometryLayers = (1 << 6) | (1 << 7);
        [SerializeField] private int agentTypeId;
        private NavMeshData data;
        private NavMeshDataInstance instance;
        public void Build()
        {
            Release();
            Physics.SyncTransforms();
            var bounds = new Bounds(transform.TransformPoint(center), size);
            var sources = new List<NavMeshBuildSource>();
            NavMeshBuilder.CollectSources(bounds, geometryLayers, NavMeshCollectGeometry.PhysicsColliders, 0, new List<NavMeshBuildMarkup>(), sources);
            data = NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByID(agentTypeId), sources, bounds, Vector3.zero, Quaternion.identity);
            if (data != null) instance = NavMesh.AddNavMeshData(data);
            else Debug.LogError("Unable to build ground navigation. Check the geometry layers and region bounds.", this);
        }
        private void Release()
        {
            if (instance.valid) instance.Remove();
            if (data != null) Destroy(data);
        }
        private void OnDestroy() => Release();
        private void OnDrawGizmosSelected() { Gizmos.color = Color.green; Gizmos.DrawWireCube(transform.TransformPoint(center), size); }
    }
}
