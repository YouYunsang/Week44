using System;
using UnityEngine;

namespace Assets.Scripts.SliceScripts
{
    class Slicer
    {
        /// <summary>
        /// Slice the object by the plane
        /// </summary>
        public static GameObject[] Slice(Plane plane, GameObject objectToCut)
        {
            Mesh mesh = objectToCut.GetComponent<MeshFilter>().mesh;
            Sliceable sliceable = objectToCut.GetComponent<Sliceable>();

            if (sliceable == null)
            {
                throw new NotSupportedException("Cannot slice non-sliceable object. Add Sliceable component first.");
            }

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

            float destroyDelay = 1.5f;
            SliceConfig config = objectToCut.GetComponent<SliceConfig>();
            if(config != null)
                destroyDelay = config.FragmentDestroyDelay;

            SetupCollidersAndRigidBodys(ref positiveObject, positiveSideMeshData, sliceable.UseGravity, destroyDelay);
            SetupCollidersAndRigidBodys(ref negativeObject, negativeSideMeshData, sliceable.UseGravity, destroyDelay);

            return new GameObject[] { positiveObject, negativeObject };
        }

        /// <summary>
        /// Creates the sliced mesh object with 2 materials:
        /// [0] original surface material
        /// [1] cap material
        /// </summary>
        private static GameObject CreateMeshGameObject(GameObject originalObject)
        {
            MeshRenderer originalRenderer = originalObject.GetComponent<MeshRenderer>();
            Sliceable originalSliceable = originalObject.GetComponent<Sliceable>();

            Material[] originalMaterials = originalRenderer.materials;
            Material surfaceMaterial = originalMaterials.Length > 0 ? originalMaterials[0] : null;
            Material capMaterial = originalSliceable.CapMaterial != null
                ? originalSliceable.CapMaterial
                : surfaceMaterial;

            GameObject meshGameObject = new GameObject();

            meshGameObject.AddComponent<MeshFilter>();
            meshGameObject.AddComponent<MeshRenderer>();

            Sliceable sliceable = meshGameObject.AddComponent<Sliceable>();
            sliceable.IsSolid = originalSliceable.IsSolid;
            sliceable.ReverseWireTriangles = originalSliceable.ReverseWireTriangles;
            sliceable.UseGravity = originalSliceable.UseGravity;
            sliceable.ShareVertices = originalSliceable.ShareVertices;
            sliceable.SmoothVertices = originalSliceable.SmoothVertices;
            sliceable.CapMaterial = originalSliceable.CapMaterial;

            meshGameObject.GetComponent<MeshRenderer>().materials = new Material[]
            {
                capMaterial,
                capMaterial
            };

            // lossyScale: 부모 포함 월드 스케일 → 새 오브젝트는 부모 없으므로 localScale에 그대로 적용
            meshGameObject.transform.localScale = originalObject.transform.lossyScale;
            meshGameObject.transform.rotation   = originalObject.transform.rotation;
            meshGameObject.transform.position   = originalObject.transform.position;
            meshGameObject.transform.rotation = originalObject.transform.rotation;
            meshGameObject.transform.position = originalObject.transform.position;
            meshGameObject.tag = originalObject.tag;

            return meshGameObject;
        }

        /// <summary>
        /// Add mesh collider and rigid body to game object
        /// </summary>
        private static void SetupCollidersAndRigidBodys(ref GameObject gameObject, Mesh mesh, bool useGravity, float destroyDelay)
        {
            Bounds bounds = mesh.bounds;
            float minSize = 0.01f;

            if (bounds.size.x > minSize &&
                bounds.size.y > minSize &&
                bounds.size.z > minSize)
            {
                //! 폴리곤 수가 너무 많으면 BoxCollider로 대체
                //! convex MeshCollider 한계 == 255 폴리곤
                if(mesh.triangles.Length / 3 <= 255)
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

            SliceFragment fragment = gameObject.AddComponent<SliceFragment>();
            fragment.Init(destroyDelay);
        }
    }
}