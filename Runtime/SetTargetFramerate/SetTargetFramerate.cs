using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetTargetFramerate : MonoBehaviour
{
    public int targetFramerate = 30;

    // Start is called before the first frame update
    void Start()
    {
        Application.targetFrameRate = targetFramerate;
    }

}
