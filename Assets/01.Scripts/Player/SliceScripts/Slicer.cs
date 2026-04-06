using System;
using UnityEngine;

namespace Assets.Scripts.SliceScripts
{
    class Slicer
    {
        public static GameObject[] Slice(Plane plane, GameObject objectToCut)
        {
            Mesh mesh = objectToCut.GetComponent<MeshFilter>().mesh;
            Sliceable sliceable = objectToCut.GetComponent<Sliceable>();

            if (sliceable == null)
                throw new NotSupportedException("Cannot slice non-sliceable object. Add Sliceable component first.");

            SlicesMetadata slicesMeta = new SlicesMetadata(
                plane,
                mesh,
                sliceable.IsSolid,
                sliceable.ReverseWireTriangles,
                sliceable.ShareVertices,
                sliceable.SmoothVertices
            );

            GameObject positiveObject = CreateMeshGameObject(objectToCut);
            positiveObject.name = $"{objectToCut.name}_positive";

            GameObject negativeObject = CreateMeshGameObject(objectToCut);
            negativeObject.name = $"{objectToCut.name}_negative";

            Mesh positiveSideMeshData = slicesMeta.PositiveSideMesh;
            Mesh negativeSideMeshData = slicesMeta.NegativeSideMesh;

            positiveObject.GetComponent<MeshFilter>().mesh = positiveSideMeshData;
            negativeObject.GetComponent<MeshFilter>().mesh = negativeSideMeshData;

            float destroyDelay = 2f;
            SliceConfig config = objectToCut.GetComponent<SliceConfig>();
            if (config != null)
                destroyDelay = config.FragmentDestroyDelay;

            SetupCollidersAndRigidBodys(ref positiveObject, positiveSideMeshData, sliceable.UseGravity, destroyDelay);
            SetupCollidersAndRigidBodys(ref negativeObject, negativeSideMeshData, sliceable.UseGravity, destroyDelay);

            return new GameObject[] { positiveObject, negativeObject };
        }

        private static GameObject CreateMeshGameObject(GameObject originalObject)
        {
            MeshRenderer originalRenderer = originalObject.GetComponent<MeshRenderer>();
            Sliceable originalSliceable = originalObject.GetComponent<Sliceable>();
            SliceConfig originalConfig = originalObject.GetComponent<SliceConfig>();

            Material[] originalSharedMaterials = originalRenderer != null
                ? originalRenderer.sharedMaterials
                : Array.Empty<Material>();

            Material surfaceMaterial = originalSharedMaterials != null && originalSharedMaterials.Length > 0
                ? originalSharedMaterials[0]
                : null;

            Material capMaterial = originalSliceable.CapMaterial != null
                ? originalSliceable.CapMaterial
                : surfaceMaterial;

            GameObject meshGameObject = new GameObject(originalObject.name + "_slicePiece");

            meshGameObject.AddComponent<MeshFilter>();
            MeshRenderer newRenderer = meshGameObject.AddComponent<MeshRenderer>();

            Sliceable sliceable = meshGameObject.AddComponent<Sliceable>();
            sliceable.IsSolid = originalSliceable.IsSolid;
            sliceable.ReverseWireTriangles = originalSliceable.ReverseWireTriangles;
            sliceable.UseGravity = originalSliceable.UseGravity;
            sliceable.ShareVertices = originalSliceable.ShareVertices;
            sliceable.SmoothVertices = originalSliceable.SmoothVertices;
            sliceable.CapMaterial = originalSliceable.CapMaterial;

            if (originalConfig != null)
            {
                SliceConfig copiedConfig = meshGameObject.AddComponent<SliceConfig>();
                CopyComponentFields(originalConfig, copiedConfig);
            }

            newRenderer.sharedMaterials = new Material[]
            {
                surfaceMaterial,
                capMaterial
            };

            Transform originalTransform = originalObject.transform;
            Transform newTransform = meshGameObject.transform;

            newTransform.position = originalTransform.position;
            newTransform.rotation = originalTransform.rotation;
            newTransform.localScale = originalTransform.lossyScale;

            meshGameObject.tag = originalObject.tag;
            meshGameObject.layer = originalObject.layer;

            return meshGameObject;
        }

        private static void SetupCollidersAndRigidBodys(ref GameObject gameObject, Mesh mesh, bool useGravity, float destroyDelay)
        {
            Bounds bounds = mesh.bounds;
            float minSize = 0.01f;

            if (bounds.size.x > minSize &&
                bounds.size.y > minSize &&
                bounds.size.z > minSize)
            {
                if (mesh.triangles.Length / 3 <= 255)
                {
                    MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();
                    meshCollider.sharedMesh = mesh;
                    meshCollider.convex = true;
                }
                else
                {
                    BoxCollider box = gameObject.AddComponent<BoxCollider>();
                    box.center = bounds.center;
                    box.size = bounds.size;
                }
            }

            Rigidbody rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = useGravity;

            SliceFragment fragment = gameObject.GetComponent<SliceFragment>();
            if (fragment == null)
                fragment = gameObject.AddComponent<SliceFragment>();

            fragment.Init(destroyDelay);
        }

        private static void CopyComponentFields<T>(T source, T destination) where T : Component
        {
            var type = typeof(T);
            var flags = System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.NonPublic;

            foreach (var field in type.GetFields(flags))
            {
                if (field.IsStatic) continue;
                field.SetValue(destination, field.GetValue(source));
            }
        }
    }
}
