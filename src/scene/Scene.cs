using System;
using System.Collections.Generic;

namespace RayTracer
{
    /// <summary>
    /// Class to represent a ray traced scene, including the objects,
    /// light sources, and associated rendering logic.
    /// </summary>
    public class Scene
    {
        private SceneOptions options;
        private ISet<SceneEntity> entities;
        private ISet<PointLight> lights;

        /// <summary>
        /// Construct a new scene with provided options.
        /// </summary>
        /// <param name="options">Options data</param>
        public Scene(SceneOptions options = new SceneOptions())
        {
            this.options = options;
            this.entities = new HashSet<SceneEntity>();
            this.lights = new HashSet<PointLight>();
        }

        /// <summary>
        /// Add an entity to the scene that should be rendered.
        /// </summary>
        /// <param name="entity">Entity object</param>
        public void AddEntity(SceneEntity entity)
        {
            this.entities.Add(entity);
        }

        /// <summary>
        /// Add a point light to the scene that should be computed.
        /// </summary>
        /// <param name="light">Light structure</param>
        public void AddPointLight(PointLight light)
        {
            this.lights.Add(light);
        }

        /// <summary>
        /// Render the scene to an output image. This is where the bulk
        /// of your ray tracing logic should go... though you may wish to
        /// break it down into multiple functions as it gets more complex!
        /// </summary>
        /// <param name="outputImage">Image to store render output</param>
        public void Render(Image outputImage)
        {
            // Begin writing your code here...    
            // Custom camera orientation

            double aspect_ratio = outputImage.Width / outputImage.Height;

            for (int y = 0; y < outputImage.Height; y++)
            {
                for (int x = 0; x < outputImage.Width; x++)
                {
                    var C = new Color(0, 0, 0);
                    double AAM = options.AAMultiplier;
                    var mult = AAM * AAM;
                    
                    double pixel_dist = 0.5f;
                    if (mult != 1) {
                        pixel_dist = 1 / AAM;
                    }
                    double x_pixel = pixel_dist;
                    double y_pixel = pixel_dist;
                
                    int turn = 0;
                    for (int i = 0; i < mult; i++) 
                    {
                        double pixel_loc_x = (x + x_pixel) / outputImage.Width;
                        double pixel_loc_y = (y + y_pixel) / outputImage.Height;
                        double pixel_loc_z = 1.0f;

                        double scale = Math.Tan((60 * 0.5) * Math.PI / 180); 

                        double x_pos = (pixel_loc_x * 2) - 1;
                        double y_pos = 1 - (pixel_loc_y * 2);

                        x_pos = x_pos * Math.Tan(Math.PI/3/2);
                        y_pos = y_pos * (Math.Tan(Math.PI/3/2) / aspect_ratio);

                        var rayOrigin = options.CameraPosition;
                        var k = options.CameraAxis;
                        var angle = options.CameraAngle;
                        var vector = new Vector3(x_pos, y_pos, pixel_loc_z);
                        var camera_vector = vector * Math.Cos(angle) + (k.Cross(vector)) * Math.Sin(angle) + k * (k.Dot(vector)) * (1-Math.Cos(angle));

                        var rayDirection = camera_vector.Normalized();
                        var ray = new Ray(rayOrigin, rayDirection);

                        int depth = 0;
                        RayHit pastHit = null;
                        C += castRay(rayOrigin, rayDirection, depth, pastHit);

                        x_pixel += pixel_dist;
                        turn += 1;
                        if (turn % options.AAMultiplier == 0) {
                            x_pixel = pixel_dist;
                            y_pixel += pixel_dist;
                        }

                    }
                    outputImage.SetPixel(x, y, C / mult);
                }
            }
            
        }

        public Color castRay(Vector3 rayOrigin, Vector3 rayDirection, int depth, RayHit pastHit) {

            RayHit nearestHit = null;
            double nearestDistance = double.PositiveInfinity;
            var C = new Color(0, 0, 0);
            foreach (SceneEntity entity in this.entities)
            {
                RayHit hit = entity.Intersect(new Ray(rayOrigin, rayDirection));

                bool notReflect = true;
                
                if (pastHit != null && hit != null) {
                    notReflect = (hit.Position - pastHit.Position).Length() < nearestDistance;
                    if (notReflect) {
                        nearestDistance = (hit.Position - pastHit.Position).Length();
                    }
                }

                if ((hit != null && (nearestHit == null || hit.Position.Length() < nearestHit.Position.Length())) && notReflect)
                {
                    nearestHit = hit;

                    // outputImage.SetPixel(x, y, C);
                    if (depth > 5) return entity.Material.Color;
                    C = new Color(0, 0, 0);
                    var N = hit.Normal;
                    var Nn = N.Normalized();

                    bool inside = false;
                    foreach (var entity3 in this.entities)
                    {
                        var hit3 = entity3.Intersect(new Ray(rayOrigin, rayDirection));
                        if (hit3 != null)
                        {
                            inside = true;
                            break;
                        }
                    }
                    
                    if (inside) {
                        if (entity.Material.Type == Material.MaterialType.Diffuse) {
                            // iterate through these lights
                            foreach (var light in this.lights)
                            {
                                // Console.WriteLine("light" + hit.Position);
                                var L = light.Position - hit.Position;
                                var Ln = L.Normalized();
                                var Cm = entity.Material.Color;
                                var Cl = light.Color;

                                bool inShadow = false;
                                foreach (var entity2 in this.entities)
                                {
                                    if (entity2 != entity)
                                    {
                                        // var hit2_pos = hit.Position + (Nn * 0.001);
                                        RayHit hit2 = entity2.Intersect(new Ray(hit.Position, Ln));
                                        // Whether the distance to the light source is greater than the blocked distance
                                        if (hit2 != null && (L.Length() > (hit2.Position - hit.Position).Length()))
                                        {
                                            inShadow = true;
                                            break;
                                        }
                                    }
                                }

                                if (!inShadow) {
                                    C += (Cm * Cl) * (Math.Max(0, Nn.Dot(Ln)));
                                }  
                            }
                        }

                        // calculate a reflective material
                        else if (entity.Material.Type == Material.MaterialType.Reflective) {
                            var reflectionDirection = reflect(hit.Incident, Nn).Normalized();
                            Color reflectionColor = new Color(0, 0, 0);

                            bool outside = (hit.Incident.Dot(Nn) < 0);
                            Vector3 bias = 0.0001 * Nn;
                            Vector3 reflectionRayOrig = outside ? hit.Position + bias : hit.Position - bias; 

                            pastHit = hit;

                            reflectionColor = castRay(reflectionRayOrig, reflectionDirection, depth + 1, pastHit); 
                            C += reflectionColor;
                        } 

                        // calculate a refractive material
                        else if (entity.Material.Type == Material.MaterialType.Refractive) {
                            Color refractionColor = new Color(0, 0, 0);
                            bool outside = (hit.Incident.Dot(Nn) < 0);
                            Vector3 bias = 0.0001 * Nn;
                            double kr = fresnel(hit.Incident, Nn, entity.Material.RefractiveIndex);

                            if (kr < 1) {
                                var refractionDirection = refract(hit.Incident, Nn, entity.Material.RefractiveIndex).Normalized();
                                Vector3 refractionRayOrig = outside ? hit.Position - bias : hit.Position + bias; 
                                double materialDis = refractDis(refractionDirection, refractionRayOrig, entity);
                                if (materialDis > 1e-6) {
                                    Color absorbance = entity.Material.Color * 1f * (- materialDis);
                                    Color transmittance = new Color(Math.Exp(absorbance.R), Math.Exp(absorbance.G), Math.Exp(absorbance.B));
                                    refractionColor = castRay(refractionRayOrig, refractionDirection, depth - 1, pastHit) * transmittance;
                                } else {
                                    refractionColor = castRay(refractionRayOrig, refractionDirection, depth - 1, pastHit); 
                                }
                            }
                            
                            Vector3 reflectionDirection = reflect(hit.Incident, N).Normalized();
                            Vector3 reflectionRayOrig = outside ? hit.Position + bias : hit.Position - bias;
                            Color reflectionColor = castRay(reflectionRayOrig, reflectionDirection, depth + 1, pastHit);

                            C += (reflectionColor * kr + refractionColor * (1 - kr));
                        } 
                    } else {
                        C += entity.Material.Color;
                    }
                }
            }
            return C;
        }

        Vector3 reflect(Vector3 I, Vector3 N) 
        { 
            return I - 2 * I.Dot(N) * N; 
        } 

        Vector3 refract(Vector3 I, Vector3 N, double ior) 
        { 
            double cosi = clamp(-1, 1, I.Dot(N)); 
            double etai = 1.0; 
            double etat = ior; 
            Vector3 n = N; 
            if (cosi < 0) { cosi = -cosi; } else { n = -N; etai = etat; etat = 1.0; } 
            double eta = etai / etat; 
            double k = 1.0 - eta * eta * (1.0 - cosi * cosi); 
            Vector3 Zero = new Vector3(0, 0, 0);
            return k < 0 ? Zero : eta * I + (eta * cosi - Math.Sqrt(k)) * n; 
        }

        public double fresnel(Vector3 rayDirection, Vector3 N, double ior)
        {   
            double kr = 0.0;
            var cosi = clamp(-1, 1, rayDirection.Dot(N));
            double etai = 1;
            double etat = ior;
            if (cosi > 0)
            {
                // swap(ref etai, ref etat);
                var temp = etai;
                etai = etat;
                etat = temp;
            }
            // Compute sini using Snell's law
            double sint = etai / etat * (double)Math.Sqrt(Math.Max(0.0f, 1 - cosi * cosi));
            // Total internal reflection
            if (sint >= 1)
            {
                kr = 1;
            }
            else
            {
                double cost = (double)Math.Sqrt(Math.Max(0.0f, 1 - sint * sint));
                cosi = Math.Abs(cosi);
                double Rs = ((etat * cosi) - (etai * cost)) / ((etat * cosi) + (etai * cost));
                double Rp = ((etai * cosi) - (etat * cost)) / ((etai * cosi) + (etat * cost));
                kr = (Rs * Rs + Rp * Rp) / 2;
                // kt = 1 - kr;
            }
            return kr;
        }

        public double clamp(double lo, double hi, double v)
        {
            return Math.Max(lo, Math.Min(hi, v));
        }

        public double refractDis(Vector3 direction, Vector3 origin, SceneEntity entity)
        {
            Ray insideRay = new Ray(origin, direction); 
            RayHit insideHit = entity.Intersect(insideRay);
            if (insideHit != null) {
                return (insideHit.Position - origin).Length();
            }
            return 0;
        }

    }
}
