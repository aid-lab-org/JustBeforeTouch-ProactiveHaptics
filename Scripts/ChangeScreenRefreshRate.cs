/*
Authors: Yihao Dong, Pamuditha Somarathne, Nisal Menuka Gamage, Anusha Withana
Contact: yihao.dong@sydney.edu.au, psom0870@uni.sydney.edu.au, nkan8027@uni.sydney.edu.au, anusha.withana@sydney.edu.au
Created: 12 Dec 2024

Description:
This script alter the screen refresh rate for meta quest.
    For other devices, please refer to the developer documentations.

License:
Creative Commons Attribution 4.0 International Public License

2024 AidLAB 
The University of Sydney
*/

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.XR.Oculus;

public class ChangeScreenRefreshRate : MonoBehaviour
{
    float TARGET_RATE = 80f;

    // Start is called before the first frame update
    void Start()
    {
        if (Performance.TrySetDisplayRefreshRate(TARGET_RATE))
        {
            Debug.Log("Setting new refresh rate to " + TARGET_RATE);
        }
    }
}