using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Building
{
    public Vector2 center;
    public float direction;
    public float height;
    public float width;
    // half of diagonal length
    public float circleColliderRadius {get;}

    public Building(Vector2 center, float height, float width, float direction) {
        this.center = center;
        this.height = height;
        this.width = width;
        this.direction = direction;

        this.circleColliderRadius = new Vector2(width/2, height/2).magnitude;
    }

    public void MakeCollider() {
        PhysicObjects.buildingsColliders.Add(this);
    }

    public void DestroyCollider() {
        PhysicObjects.buildingsColliders.Remove(this);
    }

    public void UpdateCollider() {
        DestroyCollider();
        MakeCollider();
    }
}
