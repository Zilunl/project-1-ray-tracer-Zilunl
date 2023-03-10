using System;

namespace RayTracer
{
    /// <summary>
    /// Class to represent an (infinite) plane in a scene.
    /// </summary>
    public class Sphere : SceneEntity
    {
        private Vector3 center;
        private double radius;
        private Material material;

        /// <summary>
        /// Construct a sphere given its center point and a radius.
        /// </summary>
        /// <param name="center">Center of the sphere</param>
        /// <param name="radius">Radius of the spher</param>
        /// <param name="material">Material assigned to the sphere</param>
        public Sphere(Vector3 center, double radius, Material material)
        {
            this.center = center;
            this.radius = radius;
            this.material = material;
        }

        /// <summary>
        /// Determine if a ray intersects with the sphere, and if so, return hit data.
        /// </summary>
        /// <param name="ray">Ray to check</param>
        /// <returns>Hit data (or null if no intersection)</returns>
        public RayHit Intersect(Ray ray)
        {
            // Write your code here...
            double t0 = 0;
            double t1 = 0;
            Vector3 L = ray.Origin - this.center;
            double a = ray.Direction.Dot(ray.Direction);
            double b = 2 * ray.Direction.Dot(L);
            double c = L.Dot(L) - (this.radius * this.radius);
            
            if (!solveQuadratic(a, b, c, ref t0, ref t1)) { return null; }

            if (t0 > t1) { double temp = t0; t0 = t1; t1 = temp; }

            if (t0 < 0) { 
                t0 = t1; 
                if (t0 < 0) { return null; }
            }
            Vector3 hitPoint = ray.Origin + (ray.Direction * t0);
            Vector3 normal = (hitPoint - this.center).Normalized();
            Vector3 incident = ray.Direction;
            return new RayHit(hitPoint, normal, incident, this.material);
        }

        /// <summary>
        /// The material of the sphere.
        /// </summary>
        public Material Material { get { return this.material; } }

        public bool solveQuadratic(double a, double b, double c, ref double t0, ref double t1)
        {
            double discriminant = b * b - 4 * a * c;
            if (discriminant < 0)
            {
                return false;
            } 
            else if (discriminant == 0)
            {
                t0 = -0.5 * b / a;
                t1 = t0;
            } 
            else
            {
                double q = (b > 0) ? -0.5 * (b + Math.Sqrt(discriminant)) : -0.5 * (b - Math.Sqrt(discriminant));
                t0 = q / a;
                t1 = c / q;
            }
            return true;
        }
        
    }

}
