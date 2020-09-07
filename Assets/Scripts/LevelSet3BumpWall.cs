using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class LevelSet3BumpWall : MonoBehaviour, IWallHit
{
    const float BOOST = 1.4f;

    public void Hit(Vector3 position, ref Vector3 velocity, Vector3 normal)
    {
        float extra = Ball.SPEED_LIMIT * BOOST - velocity.magnitude;
        if (extra <= 0f)
            return;

        normal = transform.InverseTransformVector(normal);
        normal.x = 0;
        normal.y = 0;
        normal = transform.TransformVector(normal);

        velocity += normal * extra;
    }
}
