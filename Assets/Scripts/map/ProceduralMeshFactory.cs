using UnityEngine;

namespace BaaroForce.Map
{
    /// <summary>Tiny procedural mesh builder — Unity's CreatePrimitive has no cone, and this
    /// project builds everything (tiles, overlays, unit sprites) in code rather than importing
    /// art assets, so terrain props (tree canopies, mountain peaks) follow the same approach.</summary>
    public static class ProceduralMeshFactory
    {
        /// <summary>
        /// A simple upward-pointing cone: a ring of <paramref name="segments"/> base vertices
        /// around the origin (Y=0) rising to a single apex at (0, height, 0). No bottom cap —
        /// these props sit flush on a tile and are never seen from below the isometric camera.
        /// Built double-sided (each face duplicated with reversed winding, on separate vertex
        /// copies so normals stay correct per copy) rather than relying on a guessed winding
        /// order, since there's no way to visually verify culling/orientation in this
        /// environment — whichever copy's winding turns out to face outward renders correctly
        /// lit; the other is simply back-face-culled away.
        /// </summary>
        public static Mesh CreateCone(float radius, float height, int segments = 10)
        {
            int ringCount = segments + 1; // ring + apex, per winding copy
            var vertices  = new Vector3[ringCount * 2];
            var triangles = new int[segments * 3 * 2];

            for (int copy = 0; copy < 2; copy++)
            {
                int vOffset    = copy * ringCount;
                int apexIndex  = vOffset + segments;
                vertices[apexIndex] = new Vector3(0f, height, 0f);

                for (int i = 0; i < segments; i++)
                {
                    float angle = 2f * Mathf.PI * i / segments;
                    vertices[vOffset + i] = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                }

                int tOffset = copy * segments * 3;
                for (int i = 0; i < segments; i++)
                {
                    int a = vOffset + i;
                    int b = vOffset + (i + 1) % segments;
                    int t = tOffset + i * 3;

                    triangles[t] = a;
                    if (copy == 0) { triangles[t + 1] = apexIndex; triangles[t + 2] = b; }
                    else            { triangles[t + 1] = b;         triangles[t + 2] = apexIndex; }
                }
            }

            var mesh = new Mesh { name = "ProceduralCone" };
            mesh.vertices  = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
