using System.Collections;
using System.Collections.Generic;
using UnityEditor.iOS.Xcode;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UIElements;

public class RollTest : MonoBehaviour
{
    private float angle = 0;
    // Start is called before the first frame update
    void Start()
    {
        
    }   

    // Update is called once per frame
    void Update()
    {
        angle -= 1f;
        transform.rotation = Quaternion.Euler(angle, 0f, 0f);
    }
}
