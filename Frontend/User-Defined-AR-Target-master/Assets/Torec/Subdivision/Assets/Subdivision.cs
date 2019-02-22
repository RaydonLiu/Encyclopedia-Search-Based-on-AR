//#define SUBDIVIDEVECTOR4 // used to subdivide meshes in 4D

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Torec
{
    public class MeshStruct // operates with mesh quad indices
    {
        public int[][] quads;       // quad index => [vertex indices]
        public int[][] vertexQuads; // vertex index => [quad indices]

        private bool m_clockwiseQuads = true;

        public MeshStruct(int vertexCount, int[][] quads, bool clockwiseQuads = true) {
            m_clockwiseQuads = clockwiseQuads;
            this.quads = quads;
            InitVertexQuads(vertexCount);
        }

        #region Triangle Utils
        public static int CheckQuads(int[] ts) {
            for (int i = 0; i < ts.Length; i += 6) {
                if (ts[i] != ts[i+3] || ts[i+2] != ts[i+4]) {
                    return i / 3; // triangle index with no quad
                }
            }
            return -1;
        }

        public static int[][] GetQuads(int[] triangles) {
            int count = triangles.Length / 6;
            var qs = new int[count][];
            for (int i = 0; i < count; ++i) {
                qs[i] = new[] {
                    triangles[6*i],
                    triangles[6*i+1],
                    triangles[6*i+2],
                    triangles[6*i+5],
                };
            }
            return qs;
        }

        public static int[] GetTriangles(int[][] quads) {
            var ts = new int[quads.Length * 6];
            for (int q = 0; q < quads.Length; ++q) {
                ts[q*6]   = quads[q][0];
                ts[q*6+1] = quads[q][1];
                ts[q*6+2] = quads[q][2];
                ts[q*6+3] = quads[q][0];
                ts[q*6+4] = quads[q][2];
                ts[q*6+5] = quads[q][3];
            }
            return ts;
        }
        #endregion

        void InitVertexQuads(int vertexCount) {
            vertexQuads = Enumerable.Range(0, vertexCount)
                .Select(v => Enumerable.Range(0, quads.Length)
                    .Where(q => quads[q].Contains(v))
                    .ToArray()
                ).ToArray();
        }

        public int[] FindSpecialPoints() {
            // returns vertices where quad count != 4
            return Enumerable.Range(0, vertexQuads.Length)
                .Where(v => vertexQuads[v].Length != 4)
                .ToArray();
        }

        public int GetIndexInQuad(int q, int v) {
            Debug.Assert(q != -1, "Invalid q index");
            return System.Array.IndexOf(quads[q], v);
        }

        public int GetNextInQuad(int q, int v, int shift = 1) {
            //  v. -> .
            //     q  v
            //   . <  .
            int i = GetIndexInQuad(q, v);
            if (!m_clockwiseQuads) shift = 4 - shift;
            return quads[q][(i + shift) % 4];
        }

        public int FindQuad(int v0, int v1) {
            foreach (int q in vertexQuads[v0]) {
                if (GetNextInQuad(q, v0) == v1) return q;
            }
            return -1;
        }

        public int[] GetAllNeighbors(int v) { // hole-safe
            var ns = new List<int>();
            foreach (int q in vertexQuads[v]) {
                int n1 = GetNextInQuad(q, v, 1);
                int n3 = GetNextInQuad(q, v, 3);
                if (!ns.Contains(n1)) ns.Add(n1);
                if (!ns.Contains(n3)) ns.Add(n3);
            }
            return ns.ToArray();
        }
    }
}

namespace Torec
{
#if !SUBDIVIDEVECTOR4
    using Vector = Vector3;
#else
    using Vector = Vector4; // to subdivide 4d meshes
#endif

    public struct Vertex {
        public Vector pos;
        public Vector normal;
        public Vector tangent;
        public Vector2 uv;
    }

    public class MeshData
    {
        public enum VertexContent {
            none    = 0,
            pos     = 1,
            normal  = 2,
            tangent = 4,
            uv      = 8,
        }

        public Vertex[] vertices;
        public MeshStruct meshStruct;
        public VertexContent vertexContent;

        public MeshData(Vertex[] vertices, int[][] quads, VertexContent vertexContent) {
            this.vertices = vertices;
            this.meshStruct = new MeshStruct(vertices.Length, quads);
            this.vertexContent = vertexContent;
        }

#if !SUBDIVIDEVECTOR4
        #region Mesh converters
        
        public static Mesh Subdivide(Mesh mesh, int iterations) { // main subdivision method
            if (iterations == 0) return mesh;
            MeshData md = new MeshData(mesh);
            md = MeshData.Subdivide(md, iterations);
            return md.CreateMesh();
        }

        public MeshData(Mesh mesh) {
            vertexContent = GetVertexContent(mesh);
            vertices = GetMeshVertices(mesh, vertexContent);
            if (MeshStruct.CheckQuads(mesh.triangles) != -1) {
                throw new UnityException("Can't create MeshData: Mesh has no quads topology. Try to 'Keep Quads' on mesh importing.");
            }
            meshStruct = new MeshStruct(mesh.vertexCount, MeshStruct.GetQuads(mesh.triangles));
        }
        public Mesh CreateMesh() {
            var mesh = new Mesh();
            // vertices
            SetMeshVertices(mesh, vertices, vertexContent);
            // triangles
            mesh.SetTriangles(MeshStruct.GetTriangles(meshStruct.quads), 0);
            return mesh;
        }

        static VertexContent GetVertexContent(Mesh mesh) {
            var content = VertexContent.none;
            if (mesh.vertices != null && mesh.vertices.Length == mesh.vertexCount) content |= VertexContent.pos;
            if (mesh.normals  != null && mesh.normals .Length == mesh.vertexCount) content |= VertexContent.normal;
            if (mesh.tangents != null && mesh.tangents.Length == mesh.vertexCount) content |= VertexContent.tangent;
            if (mesh.uv       != null && mesh.uv      .Length == mesh.vertexCount) content |= VertexContent.uv;
            return content;
        }
        static Vertex[] GetMeshVertices(Mesh mesh, VertexContent content) {
            var vs = new Vertex[mesh.vertexCount];
            for (int i = 0; i < mesh.vertexCount; ++i) {
                var v = new Vertex();
                if ((content & VertexContent.pos    ) != 0) v.pos     = mesh.vertices[i];
                if ((content & VertexContent.normal ) != 0) v.normal  = mesh.normals [i];
                if ((content & VertexContent.tangent) != 0) v.tangent = mesh.tangents[i];
                if ((content & VertexContent.uv     ) != 0) v.uv      = mesh.uv      [i];
                vs[i] = v;
            }
            return vs;
        }
        public static void SetMeshVertices(Mesh mesh, Vertex[] vs, VertexContent content) {
            if ((content & VertexContent.pos    ) != 0) mesh.SetVertices(vs.Select(v => v.pos).ToList());
            if ((content & VertexContent.normal ) != 0) mesh.SetNormals (vs.Select(v => v.normal).ToList());
            if ((content & VertexContent.tangent) != 0) mesh.SetTangents(vs.Select(v => v.tangent).Select(t => new Vector4(t.x, t.y, t.z, 1)).ToList());
            if ((content & VertexContent.uv     ) != 0) mesh.SetUVs(0, vs.Select(v => v.uv).ToList());
        }
        #endregion
#endif

        public Vertex Average(Vertex[] vs, float[] weights = null) {
            Vertex r = new Vertex();
            float ws = 0;
            for (int i = 0; i < vs.Length; ++i) {
                Vertex v = vs[i];
                float w = weights != null ? weights[i] : 1.0f;
                if ((vertexContent & VertexContent.pos    ) != 0)  r.pos     += w * v.pos;
                if ((vertexContent & VertexContent.normal ) != 0)  r.normal  += w * v.normal;
                if ((vertexContent & VertexContent.tangent) != 0)  r.tangent += w * v.tangent;
                if ((vertexContent & VertexContent.uv     ) != 0)  r.uv      += w * v.uv;
                ws += w;
            }
            if ((vertexContent & VertexContent.pos    ) != 0)  r.pos /= ws;
            if ((vertexContent & VertexContent.normal ) != 0)  r.normal.Normalize();
            if ((vertexContent & VertexContent.tangent) != 0)  r.tangent.Normalize();
            if ((vertexContent & VertexContent.uv     ) != 0)  r.uv /= ws;
            return r;
        }

        public void NormalizeVertices() {
            for (int i = 0; i < vertices.Length; ++i) {
                vertices[i].pos = vertices[i].pos.normalized;
            }
        }

        #region Catmull-Clark subdivision

        // calculation of unique keys for meshdata faces and edges
        public static int GetFacePointKey(int qi) {
            return (qi << 1) | 1;
        }
        public static int GetEdgePointKey(int vi, int vj, MeshStruct meshStruct) {
            int qi = meshStruct.FindQuad(vi, vj);
            int qj = meshStruct.FindQuad(vj, vi);
            if (qi > qj) { // quad index may be -1 (if hole)
                return (qi << 3) | (meshStruct.GetIndexInQuad(qi, vi) << 1);
            } else {
                return (qj << 3) | (meshStruct.GetIndexInQuad(qj, vj) << 1);
            }
        }

        public MeshData Subdivide()
        {
            // based on:
            // http://www.rorydriscoll.com/2008/08/01/catmull-clark-subdivision-the-basics/
            // https://rosettacode.org/wiki/Catmull%E2%80%93Clark_subdivision_surface

            var newVertices = new Utils.OrderedDictionary<int, Vertex>();

            // reserve indices for new control-points (we need no special unique keys for them)
            for (int vi = 0; vi < vertices.Length; ++vi) {
                newVertices.items.Add(default(Vertex));
            }

            // face-point
            for (int qi = 0; qi < meshStruct.quads.Length; ++qi) {
                int[] q = meshStruct.quads[qi];
                Vertex facePoint = Average(q.Select(vi => vertices[vi]).ToArray());
                int facePointKey = GetFacePointKey(qi);
                newVertices.Add(facePointKey, facePoint);
            }

            // edge-point
            var edgeMidPoints = new Dictionary<int, Vertex>();
            for (int qi = 0; qi < meshStruct.quads.Length; ++qi) {
                int[] q = meshStruct.quads[qi];
                foreach (int vi in q) {
                    int vj = meshStruct.GetNextInQuad(qi, vi);
                    int edgePointKey = GetEdgePointKey(vi, vj, meshStruct);
                    if (!newVertices.HasKey(edgePointKey)) {
                        // edge-midpoint (for control-point calculation)
                        Vertex midPoint = Average(new[] {
                            vertices[vi],
                            vertices[vj],
                        });
                        edgeMidPoints[edgePointKey] = midPoint;
                        // edge-point
                        int qj = meshStruct.FindQuad(vj, vi);
                        if (qj == -1) { // for the edges that are on the border of a hole, the edge point is just the middle of the edge.
                            newVertices.Add(edgePointKey, midPoint);
                        } else {
                            Vertex edgePoint = Average(new[] {
                                vertices[vi],
                                vertices[vj],
                                newVertices.GetItem(GetFacePointKey(qi)),
                                newVertices.GetItem(GetFacePointKey(qj)),
                            });
                            newVertices.Add(edgePointKey, edgePoint);
                        }
                    }
                }
            }

            // control-point
            for (int vi = 0; vi < vertices.Length; ++vi) {
                int[] vjs = meshStruct.GetAllNeighbors(vi);
                // check hole edges
                int[] hvjs = vjs.Where(vj => 
                    meshStruct.FindQuad(vi, vj) == -1 || 
                    meshStruct.FindQuad(vj, vi) == -1
                ).ToArray();
                if (hvjs.Any()) { // hole edge vertices
                    // edge-midpoints
                    Vertex edgeMidAverage = Average(hvjs.Select(vj =>
                        edgeMidPoints[GetEdgePointKey(vi, vj, meshStruct)]
                    ).ToArray());
                    // new control-point
                    Vertex controlPoint = Average(new[] {
                        edgeMidAverage,
                        vertices[vi]
                    });
                    newVertices.items[vi] = controlPoint; // set control-point to reserved index
                } else {
                    // edge-midpoints
                    Vertex edgeMidAverage = Average(vjs.Select(vj =>
                        edgeMidPoints[GetEdgePointKey(vi, vj, meshStruct)]
                    ).ToArray());
                    // face-points
                    Vertex faceAverage = Average(meshStruct.vertexQuads[vi].Select(qi =>
                        newVertices.GetItem(GetFacePointKey(qi))
                    ).ToArray());
                    // new control-point
                    Vertex controlPoint = Average(new[] {
                        faceAverage,
                        edgeMidAverage,
                        vertices[vi]
                    }, new[] { 1f, 2f, vjs.Length - 3f });
                    newVertices.items[vi] = controlPoint; // set control-point to reserved index
                }
            }

            // new quads
            //           eis[i]
            //     q[i].----.----.q[i+1]
            //         |         |
            // eis[i+3].  fi.    .
            //         |         |
            //         .____.____.
            int ql = meshStruct.quads.Length;
            var newQuads = new int[ql * 4][];
            for (int qi = 0; qi < ql; ++qi) {
                int[] q = meshStruct.quads[qi];
                //
                int fi = newVertices.indices[GetFacePointKey(qi)]; // face-point index
                int[] eis = new int[4]; // edge-point indices
                for (int i = 0; i < 4; ++i) {
                    eis[i] = newVertices.indices[GetEdgePointKey(q[i], q[(i + 1) % 4], meshStruct)];
                }
                for (int i = 0; i < 4; ++i) {
                    int[] qq = new int[4]; // shift indices (+ i) to keep quad orientation
                    qq[(0 + i) % 4] = q[i];
                    qq[(1 + i) % 4] = eis[i];
                    qq[(2 + i) % 4] = fi;
                    qq[(3 + i) % 4] = eis[(i + 3) % 4];
                    //newQuads[qi * 4 + i] = qq; // less fragmentized quads
                    newQuads[ql * i + qi] = qq; // more fragmentized, but we keep "first quad" (i == 0) indices in quads array
                }
            }

            MeshData md = new MeshData(newVertices.items.ToArray(), newQuads, vertexContent);
            return md;
        }

        public static MeshData Subdivide(MeshData meshData, int iterations, bool normalize = false) {
            for (int i = 0; i < iterations; ++i) {
                meshData = meshData.Subdivide();
                if (normalize) {
                    meshData.NormalizeVertices();
                }
            }
            return meshData;
        }

        #endregion Catmull-Clark subdivision
    }

    public static class Utils {
        public class OrderedDictionary<K, T> {
            public List<T> items = new List<T>();
            public Dictionary<K, int> indices = new Dictionary<K, int>();
            public void Add(K key, T item) {
                items.Add(item);
                indices[key] = items.Count - 1;
            }
            public bool HasKey(K key) {
                return indices.ContainsKey(key);
            }
            public T GetItem(K key) {
                return items[indices[key]];
            }
        }
    }

}
