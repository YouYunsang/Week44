using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.SliceScripts
{
    public enum MeshSide
    {
        Positive = 0,
        Negative = 1
    }

    class SlicesMetadata
    {
        private Mesh _positiveSideMesh;
        private List<Vector3> _positiveSideVertices;
        private List<int> _positiveSideTriangles;
        private List<int> _positiveSideCapTriangles;
        private List<Vector2> _positiveSideUvs;
        private List<Vector3> _positiveSideNormals;

        private Mesh _negativeSideMesh;
        private List<Vector3> _negativeSideVertices;
        private List<int> _negativeSideTriangles;
        private List<int> _negativeSideCapTriangles;
        private List<Vector2> _negativeSideUvs;
        private List<Vector3> _negativeSideNormals;

        private readonly List<Vector3> _pointsAlongPlane;
        private readonly Plane _plane;
        private readonly Mesh _mesh;
        private readonly bool _isSolid;
        private readonly bool _useSharedVertices;
        private readonly bool _smoothVertices;
        private readonly bool _createReverseTriangleWindings;

        public Mesh PositiveSideMesh
        {
            get
            {
                if (_positiveSideMesh == null)
                    _positiveSideMesh = new Mesh();

                SetMeshData(MeshSide.Positive);
                return _positiveSideMesh;
            }
        }

        public Mesh NegativeSideMesh
        {
            get
            {
                if (_negativeSideMesh == null)
                    _negativeSideMesh = new Mesh();

                SetMeshData(MeshSide.Negative);
                return _negativeSideMesh;
            }
        }

        public SlicesMetadata(
            Plane plane,
            Mesh mesh,
            bool isSolid,
            bool createReverseTriangleWindings,
            bool shareVertices,
            bool smoothVertices)
        {
            _positiveSideTriangles = new List<int>();
            _positiveSideCapTriangles = new List<int>();
            _positiveSideVertices = new List<Vector3>();
            _positiveSideUvs = new List<Vector2>();
            _positiveSideNormals = new List<Vector3>();

            _negativeSideTriangles = new List<int>();
            _negativeSideCapTriangles = new List<int>();
            _negativeSideVertices = new List<Vector3>();
            _negativeSideUvs = new List<Vector2>();
            _negativeSideNormals = new List<Vector3>();

            _pointsAlongPlane = new List<Vector3>();

            _plane = plane;
            _mesh = mesh;
            _isSolid = isSolid;
            _createReverseTriangleWindings = createReverseTriangleWindings;
            _useSharedVertices = shareVertices;
            _smoothVertices = smoothVertices;

            ComputeNewMeshes();
        }

        private void AddTrianglesNormalAndUvs(
            MeshSide side,
            Vector3 vertex1, Vector3? normal1, Vector2 uv1,
            Vector3 vertex2, Vector3? normal2, Vector2 uv2,
            Vector3 vertex3, Vector3? normal3, Vector2 uv3,
            bool shareVertices,
            bool isCap)
        {
            if (side == MeshSide.Positive)
            {
                AddTrianglesNormalsAndUvs(
                    ref _positiveSideVertices,
                    ref _positiveSideTriangles,
                    ref _positiveSideCapTriangles,
                    ref _positiveSideNormals,
                    ref _positiveSideUvs,
                    vertex1, normal1, uv1,
                    vertex2, normal2, uv2,
                    vertex3, normal3, uv3,
                    shareVertices,
                    isCap);
            }
            else
            {
                AddTrianglesNormalsAndUvs(
                    ref _negativeSideVertices,
                    ref _negativeSideTriangles,
                    ref _negativeSideCapTriangles,
                    ref _negativeSideNormals,
                    ref _negativeSideUvs,
                    vertex1, normal1, uv1,
                    vertex2, normal2, uv2,
                    vertex3, normal3, uv3,
                    shareVertices,
                    isCap);
            }
        }

        private void AddTrianglesNormalsAndUvs(
            ref List<Vector3> vertices,
            ref List<int> triangles,
            ref List<int> capTriangles,
            ref List<Vector3> normals,
            ref List<Vector2> uvs,
            Vector3 vertex1, Vector3? normal1, Vector2 uv1,
            Vector3 vertex2, Vector3? normal2, Vector2 uv2,
            Vector3 vertex3, Vector3? normal3, Vector2 uv3,
            bool shareVertices,
            bool isCap)
        {
            List<int> targetTriangles = isCap ? capTriangles : triangles;

            int i1 = AddOrGetVertex(ref vertices, ref normals, ref uvs, vertex1, normal1, uv1, shareVertices);
            int i2 = AddOrGetVertex(ref vertices, ref normals, ref uvs, vertex2, normal2, uv2, shareVertices);
            int i3 = AddOrGetVertex(ref vertices, ref normals, ref uvs, vertex3, normal3, uv3, shareVertices);

            targetTriangles.Add(i1);
            targetTriangles.Add(i2);
            targetTriangles.Add(i3);
        }

        private int AddOrGetVertex(
            ref List<Vector3> vertices,
            ref List<Vector3> normals,
            ref List<Vector2> uvs,
            Vector3 vertex,
            Vector3? normal,
            Vector2 uv,
            bool shareVertices)
        {
            if (shareVertices)
            {
                Vector3 safeNormal = normal ?? Vector3.up;

                for (int i = 0; i < vertices.Count; i++)
                {
                    bool samePos = (vertices[i] - vertex).sqrMagnitude < 0.0000001f;
                    bool sameUv = (uvs[i] - uv).sqrMagnitude < 0.0000001f;
                    bool sameNormal = (normals[i] - safeNormal).sqrMagnitude < 0.0001f;

                    if (samePos && sameUv && sameNormal)
                        return i;
                }
            }

            if (normal == null)
                normal = Vector3.up;

            vertices.Add(vertex);
            normals.Add(((Vector3)normal).normalized);
            uvs.Add(uv);

            return vertices.Count - 1;
        }

        private void AddReverseTriangleWinding()
        {
            int positiveVertsStartIndex = _positiveSideVertices.Count;
            _positiveSideVertices.AddRange(_positiveSideVertices);
            _positiveSideUvs.AddRange(_positiveSideUvs);
            _positiveSideNormals.AddRange(FlipNormals(_positiveSideNormals));

            int numPositiveTriangles = _positiveSideTriangles.Count;
            for (int i = 0; i < numPositiveTriangles; i += 3)
            {
                _positiveSideTriangles.Add(positiveVertsStartIndex + _positiveSideTriangles[i]);
                _positiveSideTriangles.Add(positiveVertsStartIndex + _positiveSideTriangles[i + 2]);
                _positiveSideTriangles.Add(positiveVertsStartIndex + _positiveSideTriangles[i + 1]);
            }

            int negativeVertexStartIndex = _negativeSideVertices.Count;
            _negativeSideVertices.AddRange(_negativeSideVertices);
            _negativeSideUvs.AddRange(_negativeSideUvs);
            _negativeSideNormals.AddRange(FlipNormals(_negativeSideNormals));

            int numNegativeTriangles = _negativeSideTriangles.Count;
            for (int i = 0; i < numNegativeTriangles; i += 3)
            {
                _negativeSideTriangles.Add(negativeVertexStartIndex + _negativeSideTriangles[i]);
                _negativeSideTriangles.Add(negativeVertexStartIndex + _negativeSideTriangles[i + 2]);
                _negativeSideTriangles.Add(negativeVertexStartIndex + _negativeSideTriangles[i + 1]);
            }
        }

        private Vector2 GetCutFaceUv(Vector3 point)
        {
            Vector3 n = _plane.normal.normalized;
            Vector3 u = Vector3.Cross(n, Mathf.Abs(n.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
            Vector3 v = Vector3.Cross(n, u).normalized;
            return new Vector2(Vector3.Dot(point, u), Vector3.Dot(point, v));
        }

        private void JoinPointsAlongPlane()
        {
            List<Vector3> orderedPoints = BuildOrderedCutPolygon();
            if (orderedPoints.Count < 3)
                return;

            Vector3 center = Vector3.zero;
            for (int i = 0; i < orderedPoints.Count; i++)
                center += orderedPoints[i];
            center /= orderedPoints.Count;

            Vector2 uvCenter = GetCutFaceUv(center);

            for (int i = 0; i < orderedPoints.Count; i++)
            {
                Vector3 firstVertex = orderedPoints[i];
                Vector3 secondVertex = orderedPoints[(i + 1) % orderedPoints.Count];

                Vector2 uvFirst = GetCutFaceUv(firstVertex);
                Vector2 uvSecond = GetCutFaceUv(secondVertex);

                Vector3 capNormal = ComputeNormal(center, secondVertex, firstVertex).normalized;
                float direction = Vector3.Dot(capNormal, _plane.normal);

                if (direction > 0f)
                {
                    AddTrianglesNormalAndUvs(
                        MeshSide.Positive,
                        center, -capNormal, uvCenter,
                        firstVertex, -capNormal, uvFirst,
                        secondVertex, -capNormal, uvSecond,
                        false, true);

                    AddTrianglesNormalAndUvs(
                        MeshSide.Negative,
                        center, capNormal, uvCenter,
                        secondVertex, capNormal, uvSecond,
                        firstVertex, capNormal, uvFirst,
                        false, true);
                }
                else
                {
                    AddTrianglesNormalAndUvs(
                        MeshSide.Positive,
                        center, capNormal, uvCenter,
                        secondVertex, capNormal, uvSecond,
                        firstVertex, capNormal, uvFirst,
                        false, true);

                    AddTrianglesNormalAndUvs(
                        MeshSide.Negative,
                        center, -capNormal, uvCenter,
                        firstVertex, -capNormal, uvFirst,
                        secondVertex, -capNormal, uvSecond,
                        false, true);
                }
            }
        }

        private List<Vector3> BuildOrderedCutPolygon()
        {
            List<Vector3> uniquePoints = new List<Vector3>();
            const float epsilon = 0.0001f;

            for (int i = 0; i < _pointsAlongPlane.Count; i++)
            {
                Vector3 p = _pointsAlongPlane[i];
                bool exists = false;

                for (int j = 0; j < uniquePoints.Count; j++)
                {
                    if ((uniquePoints[j] - p).sqrMagnitude < epsilon * epsilon)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                    uniquePoints.Add(p);
            }

            if (uniquePoints.Count < 3)
                return uniquePoints;

            Vector3 center = Vector3.zero;
            for (int i = 0; i < uniquePoints.Count; i++)
                center += uniquePoints[i];
            center /= uniquePoints.Count;

            Vector3 n = _plane.normal.normalized;
            Vector3 axisX = Vector3.Cross(n, Mathf.Abs(n.y) < 0.99f ? Vector3.up : Vector3.right).normalized;
            Vector3 axisY = Vector3.Cross(n, axisX).normalized;

            uniquePoints.Sort((a, b) =>
            {
                Vector3 da = a - center;
                Vector3 db = b - center;

                float angleA = Mathf.Atan2(Vector3.Dot(da, axisY), Vector3.Dot(da, axisX));
                float angleB = Mathf.Atan2(Vector3.Dot(db, axisY), Vector3.Dot(db, axisX));

                return angleA.CompareTo(angleB);
            });

            return uniquePoints;
        }

        private void SetMeshData(MeshSide side)
        {
            Mesh targetMesh;
            List<Vector3> vertices;
            List<Vector3> normals;
            List<Vector2> uvs;
            List<int> surfaceTriangles;
            List<int> capTriangles;

            if (side == MeshSide.Positive)
            {
                targetMesh = _positiveSideMesh;
                vertices = _positiveSideVertices;
                normals = _positiveSideNormals;
                uvs = _positiveSideUvs;
                surfaceTriangles = _positiveSideTriangles;
                capTriangles = _positiveSideCapTriangles;
            }
            else
            {
                targetMesh = _negativeSideMesh;
                vertices = _negativeSideVertices;
                normals = _negativeSideNormals;
                uvs = _negativeSideUvs;
                surfaceTriangles = _negativeSideTriangles;
                capTriangles = _negativeSideCapTriangles;
            }

            targetMesh.Clear();
            targetMesh.vertices = vertices.ToArray();
            targetMesh.normals = normals.ToArray();
            targetMesh.uv = uvs.ToArray();

            targetMesh.subMeshCount = 2;
            targetMesh.SetTriangles(surfaceTriangles.ToArray(), 0);
            targetMesh.SetTriangles(capTriangles.ToArray(), 1);
            targetMesh.RecalculateBounds();
        }

        private void ComputeNewMeshes()
        {
            Vector3[] meshVerts = _mesh.vertices;
            Vector3[] meshNormals = _mesh.normals;
            Vector2[] meshUvs = _mesh.uv;

            bool hasNormals = meshNormals != null && meshNormals.Length == meshVerts.Length;
            bool hasUvs = meshUvs != null && meshUvs.Length == meshVerts.Length;

            int subMeshCount = Mathf.Max(1, _mesh.subMeshCount);

            for (int subMeshIndex = 0; subMeshIndex < subMeshCount; subMeshIndex++)
            {
                int[] meshTriangles = _mesh.GetTriangles(subMeshIndex);
                bool sourceIsCap = (subMeshIndex == 1);

                for (int i = 0; i < meshTriangles.Length; i += 3)
                {
                    int vert1Index = meshTriangles[i];
                    int vert2Index = meshTriangles[i + 1];
                    int vert3Index = meshTriangles[i + 2];

                    Vector3 vert1 = meshVerts[vert1Index];
                    Vector3 vert2 = meshVerts[vert2Index];
                    Vector3 vert3 = meshVerts[vert3Index];

                    Vector2 uv1 = hasUvs ? meshUvs[vert1Index] : Vector2.zero;
                    Vector2 uv2 = hasUvs ? meshUvs[vert2Index] : Vector2.zero;
                    Vector2 uv3 = hasUvs ? meshUvs[vert3Index] : Vector2.zero;

                    Vector3 normal1 = hasNormals ? meshNormals[vert1Index] : Vector3.up;
                    Vector3 normal2 = hasNormals ? meshNormals[vert2Index] : Vector3.up;
                    Vector3 normal3 = hasNormals ? meshNormals[vert3Index] : Vector3.up;

                    bool vert1Side = _plane.GetSide(vert1);
                    bool vert2Side = _plane.GetSide(vert2);
                    bool vert3Side = _plane.GetSide(vert3);

                    if (vert1Side == vert2Side && vert2Side == vert3Side)
                    {
                        MeshSide side = vert1Side ? MeshSide.Positive : MeshSide.Negative;

                        AddTrianglesNormalAndUvs(
                            side,
                            vert1, normal1, uv1,
                            vert2, normal2, uv2,
                            vert3, normal3, uv3,
                            _useSharedVertices,
                            sourceIsCap);
                    }
                    else
                    {
                        Vector3 intersection1;
                        Vector3 intersection2;
                        Vector2 intersection1Uv;
                        Vector2 intersection2Uv;
                        Vector3 intersection1Normal;
                        Vector3 intersection2Normal;

                        MeshSide side1 = vert1Side ? MeshSide.Positive : MeshSide.Negative;
                        MeshSide side2 = vert1Side ? MeshSide.Negative : MeshSide.Positive;

                        if (vert1Side == vert2Side)
                        {
                            intersection1 = GetRayPlaneIntersectionPointAndUvAndNormal(
                                vert2, uv2, normal2,
                                vert3, uv3, normal3,
                                out intersection1Uv, out intersection1Normal);

                            intersection2 = GetRayPlaneIntersectionPointAndUvAndNormal(
                                vert3, uv3, normal3,
                                vert1, uv1, normal1,
                                out intersection2Uv, out intersection2Normal);

                            AddTrianglesNormalAndUvs(
                                side1,
                                vert1, normal1, uv1,
                                vert2, normal2, uv2,
                                intersection1, intersection1Normal, intersection1Uv,
                                _useSharedVertices,
                                sourceIsCap);

                            AddTrianglesNormalAndUvs(
                                side1,
                                vert1, normal1, uv1,
                                intersection1, intersection1Normal, intersection1Uv,
                                intersection2, intersection2Normal, intersection2Uv,
                                _useSharedVertices,
                                sourceIsCap);

                            AddTrianglesNormalAndUvs(
                                side2,
                                intersection1, intersection1Normal, intersection1Uv,
                                vert3, normal3, uv3,
                                intersection2, intersection2Normal, intersection2Uv,
                                _useSharedVertices,
                                sourceIsCap);
                        }
                        else if (vert1Side == vert3Side)
                        {
                            intersection1 = GetRayPlaneIntersectionPointAndUvAndNormal(
                                vert1, uv1, normal1,
                                vert2, uv2, normal2,
                                out intersection1Uv, out intersection1Normal);

                            intersection2 = GetRayPlaneIntersectionPointAndUvAndNormal(
                                vert2, uv2, normal2,
                                vert3, uv3, normal3,
                                out intersection2Uv, out intersection2Normal);

                            AddTrianglesNormalAndUvs(
                                side1,
                                vert1, normal1, uv1,
                                intersection1, intersection1Normal, intersection1Uv,
                                vert3, normal3, uv3,
                                _useSharedVertices,
                                sourceIsCap);

                            AddTrianglesNormalAndUvs(
                                side1,
                                intersection1, intersection1Normal, intersection1Uv,
                                intersection2, intersection2Normal, intersection2Uv,
                                vert3, normal3, uv3,
                                _useSharedVertices,
                                sourceIsCap);

                            AddTrianglesNormalAndUvs(
                                side2,
                                intersection1, intersection1Normal, intersection1Uv,
                                vert2, normal2, uv2,
                                intersection2, intersection2Normal, intersection2Uv,
                                _useSharedVertices,
                                sourceIsCap);
                        }
                        else
                        {
                            intersection1 = GetRayPlaneIntersectionPointAndUvAndNormal(
                                vert1, uv1, normal1,
                                vert2, uv2, normal2,
                                out intersection1Uv, out intersection1Normal);

                            intersection2 = GetRayPlaneIntersectionPointAndUvAndNormal(
                                vert1, uv1, normal1,
                                vert3, uv3, normal3,
                                out intersection2Uv, out intersection2Normal);

                            AddTrianglesNormalAndUvs(
                                side1,
                                vert1, normal1, uv1,
                                intersection1, intersection1Normal, intersection1Uv,
                                intersection2, intersection2Normal, intersection2Uv,
                                _useSharedVertices,
                                sourceIsCap);

                            AddTrianglesNormalAndUvs(
                                side2,
                                intersection1, intersection1Normal, intersection1Uv,
                                vert2, normal2, uv2,
                                vert3, normal3, uv3,
                                _useSharedVertices,
                                sourceIsCap);

                            AddTrianglesNormalAndUvs(
                                side2,
                                intersection1, intersection1Normal, intersection1Uv,
                                vert3, normal3, uv3,
                                intersection2, intersection2Normal, intersection2Uv,
                                _useSharedVertices,
                                sourceIsCap);
                        }

                        _pointsAlongPlane.Add(intersection1);
                        _pointsAlongPlane.Add(intersection2);
                    }
                }
            }

            if (_isSolid)
                JoinPointsAlongPlane();
            else if (_createReverseTriangleWindings)
                AddReverseTriangleWinding();

            if (_smoothVertices)
                SmoothVertices();
        }

        private Vector3 GetRayPlaneIntersectionPointAndUvAndNormal(
            Vector3 vertex1, Vector2 vertex1Uv, Vector3 normal1,
            Vector3 vertex2, Vector2 vertex2Uv, Vector3 normal2,
            out Vector2 uv,
            out Vector3 normal)
        {
            float distance = GetDistanceRelativeToPlane(vertex1, vertex2, out Vector3 pointOfIntersection);
            uv = Vector2.Lerp(vertex1Uv, vertex2Uv, distance);
            normal = Vector3.Lerp(normal1, normal2, distance).normalized;
            return pointOfIntersection;
        }

        private float GetDistanceRelativeToPlane(Vector3 vertex1, Vector3 vertex2, out Vector3 pointOfIntersection)
        {
            Ray ray = new Ray(vertex1, vertex2 - vertex1);
            _plane.Raycast(ray, out float distance);
            pointOfIntersection = ray.GetPoint(distance);

            float edgeLength = Vector3.Distance(vertex1, vertex2);
            return edgeLength > 0f ? distance / edgeLength : 0f;
        }

        private Vector3 ComputeNormal(Vector3 vertex1, Vector3 vertex2, Vector3 vertex3)
        {
            Vector3 side1 = vertex2 - vertex1;
            Vector3 side2 = vertex3 - vertex1;
            Vector3 cross = Vector3.Cross(side1, side2);
            return cross.sqrMagnitude > 1e-10f ? cross.normalized : Vector3.up;
        }

        private List<Vector3> FlipNormals(List<Vector3> currentNormals)
        {
            List<Vector3> flippedNormals = new List<Vector3>();
            foreach (Vector3 normal in currentNormals)
                flippedNormals.Add(-normal);
            return flippedNormals;
        }

        private void SmoothVertices()
        {
            DoSmoothing(ref _positiveSideVertices, ref _positiveSideNormals, ref _positiveSideTriangles);
            DoSmoothing(ref _negativeSideVertices, ref _negativeSideNormals, ref _negativeSideTriangles);
        }

        private void DoSmoothing(ref List<Vector3> vertices, ref List<Vector3> normals, ref List<int> triangles)
        {
            for (int i = 0; i < normals.Count; i++)
                normals[i] = Vector3.zero;

            for (int i = 0; i < triangles.Count; i += 3)
            {
                int v1 = triangles[i];
                int v2 = triangles[i + 1];
                int v3 = triangles[i + 2];

                Vector3 triNormal = ComputeNormal(vertices[v1], vertices[v2], vertices[v3]);
                normals[v1] += triNormal;
                normals[v2] += triNormal;
                normals[v3] += triNormal;
            }

            for (int i = 0; i < normals.Count; i++)
                normals[i] = normals[i].normalized;
        }
    }
}
