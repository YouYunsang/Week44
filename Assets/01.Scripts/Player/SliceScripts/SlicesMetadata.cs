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
        private List<Vector2> _positiveSideUvs;
        private List<Vector3> _positiveSideNormals;

        private Mesh _negativeSideMesh;
        private List<Vector3> _negativeSideVertices;
        private List<int> _negativeSideTriangles;
        private List<Vector2> _negativeSideUvs;
        private List<Vector3> _negativeSideNormals;

        private readonly List<Vector3> _pointsAlongPlane;
        private Plane _plane;
        private Mesh _mesh;
        private bool _isSolid;
        private bool _useSharedVertices = false;
        private bool _smoothVertices = false;
        private bool _createReverseTriangleWindings = false;

        public bool IsSolid
        {
            get { return _isSolid; }
            set { _isSolid = value; }
        }

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

        public SlicesMetadata(Plane plane, Mesh mesh, bool isSolid, bool createReverseTriangleWindings, bool shareVertices, bool smoothVertices)
        {
            _positiveSideTriangles = new List<int>();
            _positiveSideVertices = new List<Vector3>();
            _negativeSideTriangles = new List<int>();
            _negativeSideVertices = new List<Vector3>();
            _positiveSideUvs = new List<Vector2>();
            _negativeSideUvs = new List<Vector2>();
            _positiveSideNormals = new List<Vector3>();
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

        private void AddTrianglesNormalAndUvs(MeshSide side, Vector3 vertex1, Vector3? normal1, Vector2 uv1, Vector3 vertex2, Vector3? normal2, Vector2 uv2, Vector3 vertex3, Vector3? normal3, Vector2 uv3, bool shareVertices, bool addFirst)
        {
            if (side == MeshSide.Positive)
                AddTrianglesNormalsAndUvs(ref _positiveSideVertices, ref _positiveSideTriangles, ref _positiveSideNormals, ref _positiveSideUvs, vertex1, normal1, uv1, vertex2, normal2, uv2, vertex3, normal3, uv3, shareVertices, addFirst);
            else
                AddTrianglesNormalsAndUvs(ref _negativeSideVertices, ref _negativeSideTriangles, ref _negativeSideNormals, ref _negativeSideUvs, vertex1, normal1, uv1, vertex2, normal2, uv2, vertex3, normal3, uv3, shareVertices, addFirst);
        }

        private void AddTrianglesNormalsAndUvs(ref List<Vector3> vertices, ref List<int> triangles, ref List<Vector3> normals, ref List<Vector2> uvs, Vector3 vertex1, Vector3? normal1, Vector2 uv1, Vector3 vertex2, Vector3? normal2, Vector2 uv2, Vector3 vertex3, Vector3? normal3, Vector2 uv3, bool shareVertices, bool addFirst)
        {
            int tri1Index = vertices.IndexOf(vertex1);

            if (addFirst)
                ShiftTriangleIndeces(ref triangles);

            if (tri1Index > -1 && shareVertices)
            {
                triangles.Add(tri1Index);
            }
            else
            {
                if (normal1 == null)
                    normal1 = ComputeNormal(vertex1, vertex2, vertex3);

                int? i = addFirst ? (int?)0 : null;
                AddVertNormalUv(ref vertices, ref normals, ref uvs, ref triangles, vertex1, (Vector3)normal1, uv1, i);
            }

            int tri2Index = vertices.IndexOf(vertex2);

            if (tri2Index > -1 && shareVertices)
            {
                triangles.Add(tri2Index);
            }
            else
            {
                if (normal2 == null)
                    normal2 = ComputeNormal(vertex2, vertex3, vertex1);

                int? i = addFirst ? (int?)1 : null;
                AddVertNormalUv(ref vertices, ref normals, ref uvs, ref triangles, vertex2, (Vector3)normal2, uv2, i);
            }

            int tri3Index = vertices.IndexOf(vertex3);

            if (tri3Index > -1 && shareVertices)
            {
                triangles.Add(tri3Index);
            }
            else
            {
                if (normal3 == null)
                    normal3 = ComputeNormal(vertex3, vertex1, vertex2);

                int? i = addFirst ? (int?)2 : null;
                AddVertNormalUv(ref vertices, ref normals, ref uvs, ref triangles, vertex3, (Vector3)normal3, uv3, i);
            }
        }

        private void AddVertNormalUv(ref List<Vector3> vertices, ref List<Vector3> normals, ref List<Vector2> uvs, ref List<int> triangles, Vector3 vertex, Vector3 normal, Vector2 uv, int? index)
        {
            if (index != null)
            {
                int i = (int)index;
                vertices.Insert(i, vertex);
                uvs.Insert(i, uv);
                normals.Insert(i, normal);
                triangles.Insert(i, i);
            }
            else
            {
                vertices.Add(vertex);
                normals.Add(normal);
                uvs.Add(uv);
                triangles.Add(vertices.IndexOf(vertex));
            }
        }

        private void ShiftTriangleIndeces(ref List<int> triangles)
        {
            for (int j = 0; j < triangles.Count; j += 3)
            {
                triangles[j]     += 3;
                triangles[j + 1] += 3;
                triangles[j + 2] += 3;
            }
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

            int negativeVertextStartIndex = _negativeSideVertices.Count;
            _negativeSideVertices.AddRange(_negativeSideVertices);
            _negativeSideUvs.AddRange(_negativeSideUvs);
            _negativeSideNormals.AddRange(FlipNormals(_negativeSideNormals));

            int numNegativeTriangles = _negativeSideTriangles.Count;
            for (int i = 0; i < numNegativeTriangles; i += 3)
            {
                _negativeSideTriangles.Add(negativeVertextStartIndex + _negativeSideTriangles[i]);
                _negativeSideTriangles.Add(negativeVertextStartIndex + _negativeSideTriangles[i + 2]);
                _negativeSideTriangles.Add(negativeVertextStartIndex + _negativeSideTriangles[i + 1]);
            }
        }

        /// <summary>
        /// [Fix] 단면 UV를 평면 탄젠트 기저로 플래너 매핑하여 계산
        /// </summary>
        private Vector2 GetCutFaceUv(Vector3 point)
        {
            Vector3 n = _plane.normal.normalized;
            Vector3 u = Vector3.Cross(n, Mathf.Abs(n.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
            Vector3 v = Vector3.Cross(n, u).normalized;
            return new Vector2(Vector3.Dot(point, u), Vector3.Dot(point, v));
        }

        private void JoinPointsAlongPlane()
        {
            Vector3 halfway = GetHalfwayPoint(out float distance);

            for (int i = 0; i < _pointsAlongPlane.Count; i += 2)
            {
                Vector3 firstVertex  = _pointsAlongPlane[i];
                Vector3 secondVertex = _pointsAlongPlane[i + 1];

                // [Fix] Vector2.zero 대신 평면 기반 플래너 UV 계산
                Vector2 uvHalf   = GetCutFaceUv(halfway);
                Vector2 uvFirst  = GetCutFaceUv(firstVertex);
                Vector2 uvSecond = GetCutFaceUv(secondVertex);

                Vector3 normal3 = ComputeNormal(halfway, secondVertex, firstVertex);
                normal3.Normalize();

                var direction = Vector3.Dot(normal3, _plane.normal);

                if (direction > 0)
                {
                    AddTrianglesNormalAndUvs(MeshSide.Positive, halfway, -normal3, uvHalf,  firstVertex,  -normal3, uvFirst,  secondVertex, -normal3, uvSecond, false, true);
                    AddTrianglesNormalAndUvs(MeshSide.Negative, halfway,  normal3, uvHalf,  secondVertex,  normal3, uvSecond, firstVertex,   normal3, uvFirst,  false, true);
                }
                else
                {
                    AddTrianglesNormalAndUvs(MeshSide.Positive, halfway,  normal3, uvHalf,  secondVertex,  normal3, uvSecond, firstVertex,   normal3, uvFirst,  false, true);
                    AddTrianglesNormalAndUvs(MeshSide.Negative, halfway, -normal3, uvHalf,  firstVertex,  -normal3, uvFirst,  secondVertex, -normal3, uvSecond, false, true);
                }
            }
        }

        private Vector3 GetHalfwayPoint(out float distance)
        {
            if (_pointsAlongPlane.Count > 0)
            {
                Vector3 firstPoint   = _pointsAlongPlane[0];
                Vector3 furthestPoint = Vector3.zero;
                distance = 0f;

                foreach (Vector3 point in _pointsAlongPlane)
                {
                    float currentDistance = Vector3.Distance(firstPoint, point);
                    if (currentDistance > distance)
                    {
                        distance      = currentDistance;
                        furthestPoint = point;
                    }
                }

                return Vector3.Lerp(firstPoint, furthestPoint, 0.5f);
            }
            else
            {
                distance = 0;
                return Vector3.zero;
            }
        }

        private void SetMeshData(MeshSide side)
        {
            if (side == MeshSide.Positive)
            {
                _positiveSideMesh.vertices  = _positiveSideVertices.ToArray();
                _positiveSideMesh.triangles = _positiveSideTriangles.ToArray();
                _positiveSideMesh.normals   = _positiveSideNormals.ToArray();
                _positiveSideMesh.uv        = _positiveSideUvs.ToArray();
            }
            else
            {
                _negativeSideMesh.vertices  = _negativeSideVertices.ToArray();
                _negativeSideMesh.triangles = _negativeSideTriangles.ToArray();
                _negativeSideMesh.normals   = _negativeSideNormals.ToArray();
                _negativeSideMesh.uv        = _negativeSideUvs.ToArray();
            }
        }

        private void ComputeNewMeshes()
        {
            int[]     meshTriangles = _mesh.triangles;
            Vector3[] meshVerts    = _mesh.vertices;
            Vector3[] meshNormals  = _mesh.normals;
            Vector2[] meshUvs      = _mesh.uv;

            // [Fix] Array.IndexOf O(n²) → Dictionary O(1)
            var vertIndexMap = new Dictionary<Vector3, int>();
            for (int i = 0; i < meshVerts.Length; i++)
            {
                if (!vertIndexMap.ContainsKey(meshVerts[i]))
                    vertIndexMap[meshVerts[i]] = i;
            }

            for (int i = 0; i < meshTriangles.Length; i += 3)
            {
                Vector3 vert1 = meshVerts[meshTriangles[i]];
                int vert1Index = vertIndexMap.TryGetValue(vert1, out int idx1) ? idx1 : 0;
                Vector2 uv1     = meshUvs[vert1Index];
                Vector3 normal1 = meshNormals[vert1Index];
                bool vert1Side  = _plane.GetSide(vert1);

                Vector3 vert2 = meshVerts[meshTriangles[i + 1]];
                int vert2Index = vertIndexMap.TryGetValue(vert2, out int idx2) ? idx2 : 0;
                Vector2 uv2     = meshUvs[vert2Index];
                Vector3 normal2 = meshNormals[vert2Index];
                bool vert2Side  = _plane.GetSide(vert2);

                Vector3 vert3 = meshVerts[meshTriangles[i + 2]];
                int vert3Index = vertIndexMap.TryGetValue(vert3, out int idx3) ? idx3 : 0;
                Vector3 normal3 = meshNormals[vert3Index];
                Vector2 uv3     = meshUvs[vert3Index];
                bool vert3Side  = _plane.GetSide(vert3);

                if (vert1Side == vert2Side && vert2Side == vert3Side)
                {
                    MeshSide side = vert1Side ? MeshSide.Positive : MeshSide.Negative;
                    AddTrianglesNormalAndUvs(side, vert1, normal1, uv1, vert2, normal2, uv2, vert3, normal3, uv3, true, false);
                }
                else
                {
                    Vector3 intersection1, intersection2;
                    Vector2 intersection1Uv, intersection2Uv;

                    MeshSide side1 = vert1Side ? MeshSide.Positive : MeshSide.Negative;
                    MeshSide side2 = vert1Side ? MeshSide.Negative : MeshSide.Positive;

                    if (vert1Side == vert2Side)
                    {
                        intersection1 = GetRayPlaneIntersectionPointAndUv(vert2, uv2, vert3, uv3, out intersection1Uv);
                        intersection2 = GetRayPlaneIntersectionPointAndUv(vert3, uv3, vert1, uv1, out intersection2Uv);

                        AddTrianglesNormalAndUvs(side1, vert1, null, uv1, vert2, null, uv2, intersection1, null, intersection1Uv, _useSharedVertices, false);
                        AddTrianglesNormalAndUvs(side1, vert1, null, uv1, intersection1, null, intersection1Uv, intersection2, null, intersection2Uv, _useSharedVertices, false);
                        AddTrianglesNormalAndUvs(side2, intersection1, null, intersection1Uv, vert3, null, uv3, intersection2, null, intersection2Uv, _useSharedVertices, false);
                    }
                    else if (vert1Side == vert3Side)
                    {
                        intersection1 = GetRayPlaneIntersectionPointAndUv(vert1, uv1, vert2, uv2, out intersection1Uv);
                        intersection2 = GetRayPlaneIntersectionPointAndUv(vert2, uv2, vert3, uv3, out intersection2Uv);

                        AddTrianglesNormalAndUvs(side1, vert1, null, uv1, intersection1, null, intersection1Uv, vert3, null, uv3, _useSharedVertices, false);
                        AddTrianglesNormalAndUvs(side1, intersection1, null, intersection1Uv, intersection2, null, intersection2Uv, vert3, null, uv3, _useSharedVertices, false);
                        AddTrianglesNormalAndUvs(side2, intersection1, null, intersection1Uv, vert2, null, uv2, intersection2, null, intersection2Uv, _useSharedVertices, false);
                    }
                    else
                    {
                        intersection1 = GetRayPlaneIntersectionPointAndUv(vert1, uv1, vert2, uv2, out intersection1Uv);
                        intersection2 = GetRayPlaneIntersectionPointAndUv(vert1, uv1, vert3, uv3, out intersection2Uv);

                        AddTrianglesNormalAndUvs(side1, vert1, null, uv1, intersection1, null, intersection1Uv, intersection2, null, intersection2Uv, _useSharedVertices, false);
                        AddTrianglesNormalAndUvs(side2, intersection1, null, intersection1Uv, vert2, null, uv2, vert3, null, uv3, _useSharedVertices, false);
                        AddTrianglesNormalAndUvs(side2, intersection1, null, intersection1Uv, vert3, null, uv3, intersection2, null, intersection2Uv, _useSharedVertices, false);
                    }

                    _pointsAlongPlane.Add(intersection1);
                    _pointsAlongPlane.Add(intersection2);
                }
            }

            if (_isSolid)
                JoinPointsAlongPlane();
            else if (_createReverseTriangleWindings)
                AddReverseTriangleWinding();

            if (_smoothVertices)
                SmoothVertices();
        }

        private Vector3 GetRayPlaneIntersectionPointAndUv(Vector3 vertex1, Vector2 vertex1Uv, Vector3 vertex2, Vector2 vertex2Uv, out Vector2 uv)
        {
            float distance = GetDistanceRelativeToPlane(vertex1, vertex2, out Vector3 pointOfIntersection);
            uv = InterpolateUvs(vertex1Uv, vertex2Uv, distance);
            return pointOfIntersection;
        }

        /// <summary>
        /// [Fix] Ray가 방향을 정규화하므로 distance는 월드 단위 거리 → 엣지 길이로 나눠 0~1 t값으로 변환
        /// </summary>
        private float GetDistanceRelativeToPlane(Vector3 vertex1, Vector3 vertex2, out Vector3 pointOfintersection)
        {
            Ray ray = new Ray(vertex1, vertex2 - vertex1);
            _plane.Raycast(ray, out float distance);
            pointOfintersection = ray.GetPoint(distance);

            float edgeLength = Vector3.Distance(vertex1, vertex2);
            return edgeLength > 0f ? distance / edgeLength : 0f;
        }

        private Vector2 InterpolateUvs(Vector2 uv1, Vector2 uv2, float distance)
        {
            return Vector2.Lerp(uv1, uv2, distance);
        }

        private Vector3 ComputeNormal(Vector3 vertex1, Vector3 vertex2, Vector3 vertex3)
        {
            Vector3 side1  = vertex2 - vertex1;
            Vector3 side2  = vertex3 - vertex1;
            Vector3 cross  = Vector3.Cross(side1, side2);
            // 퇴화 삼각형(cross≈0)이면 NaN 노멀 → 블룸 폭발 방지
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

        /// <summary>
        /// [Fix] ForEach 람다는 값 복사라 원본 수정 불가 → for 루프로 교체
        /// </summary>
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
