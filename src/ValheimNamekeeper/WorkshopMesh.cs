using System.Collections.Generic;
using UnityEngine;

namespace SuperNova.ValheimNamekeeper
{
    internal static class WorkshopMesh
    {
        private static Mesh _box;
        internal static Mesh Box => _box != null ? _box : (_box = CreateBox());
        private static Mesh CreateBox()
        {
            const float inset = .46f;
            var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            void Face(params Vector3[] points)
            {
                var center = Vector3.zero; foreach (var point in points) center += point;
                if (Vector3.Dot(Vector3.Cross(points[1] - points[0], points[2] - points[0]), center) < 0)
                    System.Array.Reverse(points);
                int start = vertices.Count;
                foreach (var point in points) { vertices.Add(point); uv.Add(new Vector2(point.x + point.z, point.y + point.z)); }
                for (int i = 1; i < points.Length - 1; i++) { triangles.Add(start); triangles.Add(start + i); triangles.Add(start + i + 1); }
            }
            var axes = new[] { Vector3.right, Vector3.up, Vector3.forward };
            for (int i = 0; i < 3; i++)
            foreach (float sign in new[] { -1f, 1f })
            {
                var normal = axes[i] * (.5f * sign); var u = axes[(i + 1) % 3] * inset; var v = axes[(i + 2) % 3] * inset;
                Face(normal - u - v, normal + u - v, normal + u + v, normal - u + v);
            }
            for (int i = 0; i < 3; i++)
            for (int j = i + 1; j < 3; j++)
            foreach (float a in new[] { -1f, 1f })
            foreach (float b in new[] { -1f, 1f })
            {
                var u = axes[i] * a; var v = axes[j] * b; var edge = axes[3 - i - j] * inset;
                Face(u * .5f + v * inset - edge, u * .5f + v * inset + edge,
                    u * inset + v * .5f + edge, u * inset + v * .5f - edge);
            }
            foreach (float x in new[] { -1f, 1f })
            foreach (float y in new[] { -1f, 1f })
            foreach (float z in new[] { -1f, 1f })
                Face(new Vector3(.5f*x, inset*y, inset*z), new Vector3(inset*x, .5f*y, inset*z), new Vector3(inset*x, inset*y, .5f*z));
            var mesh = new Mesh { name = "NamekeeperBeveledTimber" };
            mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }
    }
}
