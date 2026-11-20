using UnityEngine;

// La physique du boulet (GDD section 4), partagee par le vol ET les pointilles de visee :
// les pointilles montrent donc exactement la vraie trajectoire.
// Axe X = la piste, Y = la hauteur, Z = gauche/droite.
public static class BallPhysics
{
    [System.Serializable]
    public class Params
    {
        public float gravity = 9.81f;
        public float drag = 0.10f;            // Aerodynamisme
        public float maxSpeed = 250f;
        public float bounce = 0.45f;          // Rebond : vitesse VERTICALE gardee
        public float groundFriction = 0.9f;   // vitesse HORIZONTALE gardee a chaque rebond
        public float rollThreshold = 0.3f;    // m de rebond en dessous duquel on roule
        public float rollBrake = 1.5f;        // m/s2
        public float radius = 0.3f;
    }

    // Un pas de simulation. corridorHalfWidth : demi-largeur du couloir ; ceiling <= 0 = pas de plafond.
    // Renvoie vrai si le boulet a touche le sol pendant ce pas.
    public static bool Step(ref Vector3 position, ref Vector3 velocity, ref bool rolling,
                            Params p, float dt, float corridorHalfWidth, float ceiling)
    {
        velocity *= 1f - p.drag * dt;
        if (!rolling)
        {
            velocity.y -= p.gravity * dt;
        }
        velocity = Vector3.ClampMagnitude(velocity, p.maxSpeed);
        position += velocity * dt;

        // Murs invisibles sur les cotes
        float half = corridorHalfWidth - p.radius;
        if (Mathf.Abs(position.z) > half)
        {
            position.z = Mathf.Sign(position.z) * half;
            velocity.z = -velocity.z * 0.5f;
        }
        // Plafond (facultatif)
        if (ceiling > 0f && position.y > ceiling - p.radius)
        {
            position.y = ceiling - p.radius;
            velocity.y = -Mathf.Abs(velocity.y) * 0.5f;
        }

        // Sol plat en y = 0
        if (position.y > p.radius)
        {
            return false;
        }
        position.y = p.radius;
        if (rolling)
        {
            Vector3 flat = new Vector3(velocity.x, 0f, velocity.z);
            float speed = Mathf.Max(0f, flat.magnitude - p.rollBrake * dt);
            velocity = flat.normalized * speed;
        }
        else if (velocity.y < 0f)
        {
            velocity = new Vector3(velocity.x * p.groundFriction, -velocity.y * p.bounce,
                                   velocity.z * p.groundFriction);
            if (velocity.y * velocity.y / (2f * p.gravity) < p.rollThreshold)
            {
                rolling = true;
                velocity.y = 0f;
            }
        }
        return true;
    }
}
