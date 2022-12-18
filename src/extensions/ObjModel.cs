using System;
using System.IO;
using System.Collections;

namespace RayTracer
{
    /// <summary>
    /// Add-on option C. You should implement your solution in this class template.
    /// </summary>
    public class ObjModel : SceneEntity
    {
        private Material material;
        private ArrayList vertices = new ArrayList();
        private ArrayList normals = new ArrayList();
        private ArrayList faces = new ArrayList();

        private Sphere objSphere = null;

        /// <summary>
        /// Construct a new OBJ model.
        /// </summary>
        /// <param name="objFilePath">File path of .obj</param>
        /// <param name="offset">Vector each vertex should be offset by</param>
        /// <param name="scale">Uniform scale applied to each vertex</param>
        /// <param name="material">Material applied to the model</param>
        public ObjModel(string objFilePath, Vector3 offset, double scale, Material material)
        {
            this.material = material;

            // Here's some code to get you started reading the file...
            string[] lines = File.ReadAllLines(objFilePath);
            for (int i = 0; i < lines.Length; i++)
            {
                // The current line is lines[i]
                if (lines[i].StartsWith("v "))
                {
                    // This line is a vertex
                    string[] parts = lines[i].Split(' ');
                    this.vertices.Add(new Vector3(
                        double.Parse(parts[1]) * scale + offset.X,
                        double.Parse(parts[2]) * scale + offset.Y,
                        - double.Parse(parts[3]) * scale + offset.Z));
                } else if (lines[i].StartsWith("vn "))
                {
                    // This line is a normal
                    string[] parts = lines[i].Split(' ');
                    this.normals.Add(new Vector3(
                        double.Parse(parts[1]),
                        double.Parse(parts[2]),
                        - double.Parse(parts[3])));
                    // Console.WriteLine("Normal: " + parts[1]);
                } 
                else if (lines[i].StartsWith("f "))
                {
                    // This line is a face
                    string[] parts = lines[i].Split(' ');
                    this.faces.Add(new Vector3(
                        double.Parse(parts[3].Split('/')[0]),
                        double.Parse(parts[2].Split('/')[0]),
                        double.Parse(parts[1].Split('/')[0])));
                }
            }
            double x_min = 0.0; double x_max = 0.0;
            double y_min = 0.0; double y_max = 0.0;
            double z_min = 0.0; double z_max = 0.0;
            foreach (var vertex in this.vertices)
            {
                var vertex_x = ((Vector3)vertex).X;
                var vertex_y = ((Vector3)vertex).Y;
                var vertex_z = ((Vector3)vertex).Z;
                if (vertex_x < x_min) {x_min = vertex_x;}
                if (vertex_x > x_max) {x_max = vertex_x;}
                if (vertex_y < y_min) {y_min = vertex_y;}
                if (vertex_y > y_max) {y_max = vertex_y;}
                if (vertex_z < z_min) {z_min = vertex_z;}
                if (vertex_z > z_max) {z_max = vertex_z;}
            }
            var sphereX = (x_max + x_min) / 2;
            var sphereY = (y_max + y_min) / 2 ;
            var sphereZ = (z_max + z_min) / 2;

            double radius = (x_max - x_min) / 2;
            if ((y_max - y_min) / 2 > radius) {radius = (y_max - y_min) / 2;}
            if ((z_max - z_min) / 2 > radius) {radius = (z_max - z_min) / 2;}
            this.objSphere = new Sphere(new Vector3(sphereX, sphereY, sphereZ), radius, this.material);
        }

        /// <summary>
        /// Given a ray, determine whether the ray hits the object
        /// and if so, return relevant hit data (otherwise null).
        /// </summary>
        /// <param name="ray">Ray data</param>
        /// <returns>Ray hit data, or null if no hit</returns>
        public RayHit Intersect(Ray ray)
        {
            // Write your code here...

            var sphereInter = this.objSphere.Intersect(ray);

            var shortDis = -1.0;
            RayHit shortHit = null;

            if (sphereInter != null)
            {
                foreach (Vector3 face in this.faces)
                {
                    var v0 = (Vector3)this.vertices[(int)face.X - 1];
                    var v1 = (Vector3)this.vertices[(int)face.Y - 1];
                    var v2 = (Vector3)this.vertices[(int)face.Z - 1];

                    // vertex normal
                    var vn0 = (Vector3)this.normals[(int)face.X - 1];
                    var vn1 = (Vector3)this.normals[(int)face.Y - 1];
                    var vn2 = (Vector3)this.normals[(int)face.Z - 1];

                    var t = double.PositiveInfinity;
                    var hit = IntersectTriangle(ray, v0, v1, v2, vn0, vn1, vn2, ref t);

                    if (hit != null) {
                        if (shortDis < 0) {
                            shortDis = (hit.Position - ray.Origin).Length();
                            shortHit = hit;
                        } else {
                            if (shortDis < (hit.Position - ray.Origin).Length()) {
                                continue;
                            } else {
                                shortDis = (hit.Position - ray.Origin).Length();
                                shortHit = hit;
                            }
                        }
                    }

                }
            }
            return shortHit;
        }

        /// <summary>

        /// The material attached to this object.
        /// </summary>
        public Material Material { get { return this.material; } }

        public Sphere ObjSphere { get { return this.objSphere; } }

        public RayHit IntersectTriangle(Ray ray, Vector3 v0, Vector3 v1, Vector3 v2, Vector3 n0, Vector3 n1, Vector3 n2, ref double t)
        {
            // Write your code here...
            var v0v1 = v1 - v0;
            var v0v2 = v2 - v0;
            var pvec = ray.Direction.Cross(v0v2);
            var det = v0v1.Dot(pvec);
            
            if (Math.Abs(det) < 0.00001) return null;

            var invDet = 1.0 / det;

            var tvec = ray.Origin - v0;
            var u = tvec.Dot(pvec) * invDet;
            if (u < 0 || u > 1) return null;

            var qvec = tvec.Cross(v0v1);
            var v = ray.Direction.Dot(qvec) * invDet;
            if (v < 0 || u + v > 1) return null;

            t = v0v2.Dot(qvec) * invDet;

            if (t > 0.00001) {
                var position = ray.Origin + ray.Direction * t;
                var normal = (1 - u - v) * n0 + u * n1 + v * n2;
                normal.Normalized();
                var incident = ray.Direction.Normalized();
                return new RayHit(position, normal, incident, this.material);
            }

            return null;
        }
    }

}
