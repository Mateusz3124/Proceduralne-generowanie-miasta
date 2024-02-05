using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

public class SplineMeshClient : MonoBehaviour
{
    void Start()
    {
        
    }

    void Update() {
        if(one_time) {
            one_time = false;
            sm.CreateMesh(GetComponent<SplineContainer>(), transform);
        }
    }

    [SerializeField] SplineMesh sm;
    bool one_time = true;
}
