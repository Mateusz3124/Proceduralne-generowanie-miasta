using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Building
{
    private Vector2 center;
    private float direction;
    // private List<Vector2> corners = new List<Vector2>();

    public Building(Vector2 center, float direction) {
        this.center = center;
        this.direction = direction;
    }

    public void MakeCollider() {
        // this.GenerateCorners();
        PhysicObjects.buildingsColliders.Add(this);
    }

    public void DestroyCollider() {
        PhysicObjects.buildingsColliders.Remove(this);
    }

    public void UpdateCollider() {
        DestroyCollider();
        MakeCollider();
    }

    // public void GenerateCorners()
    // {
    // }
}
