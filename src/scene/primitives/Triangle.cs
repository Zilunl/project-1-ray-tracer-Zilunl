using System;

namespace RayTracer
{
    /// <summary>
    /// Class to represent a triangle in a scene represented by three vertices.
    /// </summary>
    public class Triangle : SceneEntity
    {
        private Vector3 v0, v1, v2;
        private Material material;

        /// <summary>
        /// Construct a triangle object given three vertices.
        /// </summary>
        /// <param name="v0">First vertex position</param>
        /// <param name="v1">Second vertex position</param>
        /// <param name="v2">Third vertex position</param>
        /// <param name="material">Material assigned to the triangle</param>
        public Triangle(Vector3 v0, Vector3 v1, Vector3 v2, Material material)
        {
            this.v0 = v0;
            this.v1 = v1;
            this.v2 = v2;
            this.material = material;
        }

        /// <summary>
        /// Determine if a ray intersects with the triangle, and if so, return hit data.
        /// </summary>
        /// <param name="ray">Ray to check</param>
        /// <returns>Hit data (or null if no intersection)</returns>
        public RayHit Intersect(Ray ray)
        {
            // Write your code here...
            var e1 = this.v1 - this.v0;
            var e2 = this.v2 - this.v0;
            var p = ray.Direction.Cross(e2);
            var a = e1.Dot(p);
            if (a > -0.00001 && a < 0.00001)
            {
                return null;
            }
            var f = 1 / a;
            var s = ray.Origin - this.v0;
            var u = f * s.Dot(p);
            if (u < 0 || u > 1)
            {
                return null;
            }
            var q = s.Cross(e1);
            var v = f * ray.Direction.Dot(q);
            if (v < 0 || u + v > 1)
            {
                return null;
            }
            var t = f * e2.Dot(q);
            if (t > 0.00001)
            {
                var position = ray.Origin + ray.Direction * t;
                var normal = (e1.Cross(e2));
                normal.Normalized();
                var incident = ray.Direction;
                return new RayHit(position, normal, incident, this.material);
            }
            return null;
        }

        /// <summary>
        /// The material of the triangle.
        /// </summary>
        public Material Material { get { return this.material; } }
    }

}
