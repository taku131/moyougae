using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spiner : MonoBehaviour
{
    
    [SerializeField] float speed = 180f; // “x/•b

    void Update()
    {
        transform.Rotate(0, 0, -speed * Time.deltaTime);
    }


}
