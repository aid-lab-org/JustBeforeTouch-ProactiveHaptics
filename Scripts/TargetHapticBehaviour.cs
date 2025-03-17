/*

*/

/*
Authors: Yihao Dong, Pamuditha Somarathne, Nisal Menuka Gamage, Anusha Withana
Contact: yihao.dong@sydney.edu.au, psom0870@uni.sydney.edu.au, nkan8027@uni.sydney.edu.au, anusha.withana@sydney.edu.au
Created: 12 Dec 2024

Description:
This script procide haptic cues then hapticTrigger enter the attached GameObject.
    OVRInput.SetControllerVibration() is used, may change to the device sepecific function for non quest devices.

Usage:
Refer to the comments for the usage of parameters.

License:
Creative Commons Attribution 4.0 International Public License

2024 AidLAB 
The University of Sydney
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetHapticBehaviour : MonoBehaviour
{
    public bool leftHand = false; // if true, the left hand controller will provide haptic cues.
    public bool rightHand = true; // if true, the right hand controller will provide haptic cues.
    public GameObject visualTrigger; // trigger which stops haptic for non pulse pattens.
    public GameObject hapticTrigger; // trigger which start the haptic cues, the GameObject needs to include the ProactiveHapticsTrigger script for hand motion depends haptic pattens.
    private ProactiveHapticsTrigger proactiveHapticsTrigger; // fetch the hand motion from the prediction algrism to provide hand motion adjuested pattens.

    public bool useConstant = true;
    public bool usePulse = false;
    public bool useIncreasing = false;
    public bool useDecreasing = false;
    public bool usePositive = false;
    public bool useNegative = false;


    private float hapticFrequency = 0.3f;
    private float hapticAmplitude = 0.5f;
    private float dynamicHapticStartF = 0.3f;
    private float dynamicHapticStartA = 0.5f;
    private float dynamicHapticChangeRate = 0.04f;


    private bool useHapticDuration = false;
    private float hapticDuration = 0.1f;
    private bool triggeredHaptic = false;
    private float minHapticGap = 0.5f;
    private bool isDynamicHapticOn = false;
    private bool useDynamicHaptic = false;
    private bool useDirection = false;
    private bool useAcceloation = false;
    private bool isIncreasing = false;
    private bool isPositive = false;

    private void Start()
    {
        configHapticPatterns();
        resetDynamicHaptic();
        proactiveHapticsTrigger = hapticTrigger.GetComponent<ProactiveHapticsTrigger>();

        if (useDirection && isIncreasing) dynamicHapticStartA = 0f;
        if (useDirection && !isIncreasing) dynamicHapticStartA = 1f;
    }

    private void FixedUpdate()
    {

        if (isDynamicHapticOn){
            if (useDirection) ProvideDynamicHaptic();
            else if (useAcceloation) ProvideAccelerationAdjuestedDynamicHaptic(dynamicHapticStartF, dynamicHapticStartA, isPositive);
        }
        
    }

    private void configHapticPatterns()
    {
        useHapticDuration = false;
        useDynamicHaptic = false;
        useDirection = false;
        useAcceloation = false;
        
        if (usePulse) {
            useHapticDuration = true;
        }
        else if (useIncreasing) {
            useDynamicHaptic = true;
            useDirection = true;
            isIncreasing = true;
        }
        else if (useDecreasing) {
            useDynamicHaptic = true;
            useDirection = true;
            isIncreasing = false;
        }
        else if (usePositive) {
            useDynamicHaptic = true;
            useAcceloation = true;
            isPositive = true;
        }
        else if (useNegative) {
            useDynamicHaptic = true;
            useAcceloation = true;
            isPositive = false;
        }
    }

    private void resetDynamicHaptic()
    {
        isDynamicHapticOn = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == hapticTrigger && !triggeredHaptic)
        {
            if (useHapticDuration)
            {
                StartCoroutine(TimedHaptics(hapticFrequency, hapticAmplitude, hapticDuration));
            }
            else if (useDynamicHaptic)
            {
                isDynamicHapticOn = true;
            }
            else
            {
                StartContinuesHaptic(hapticFrequency, hapticAmplitude);
            }

            StartCoroutine(stopDetactHapticEnterForSec());
        }
    }

    IEnumerator stopDetactHapticEnterForSec()
    {
        triggeredHaptic = true;
        yield return new WaitForSeconds(minHapticGap);
        triggeredHaptic = false;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject == visualTrigger && !useHapticDuration)
        {
            StopHaptic();
        }
    }

    IEnumerator TimedHaptics(float frequency, float amplitude, float duration)
    {
        StartContinuesHaptic(frequency, amplitude);
        yield return new WaitForSeconds(duration);
        StopHaptic();
    }

    private void StartContinuesHaptic(float frequency, float amplitude)
    {
        if (leftHand) OVRInput.SetControllerVibration(frequency, amplitude, OVRInput.Controller.LTouch);
        if (rightHand) OVRInput.SetControllerVibration(frequency, amplitude, OVRInput.Controller.RTouch);
    }

    private void ProvideDynamicHaptic()
    {
        if (isIncreasing) dynamicHapticStartA = Mathf.Max(0f, Mathf.Min(dynamicHapticStartA + dynamicHapticChangeRate, 1f));
        else dynamicHapticStartA = Mathf.Max(0f, Mathf.Min(dynamicHapticStartA - dynamicHapticChangeRate, 1f));

        StartContinuesHaptic(dynamicHapticStartF, dynamicHapticStartA);
        Debug.Log("amplitude\t" + dynamicHapticStartA);
    }

    private void ProvideAccelerationAdjuestedDynamicHaptic(float frequency, float amplitude, bool isPositive)
    {
        Vector3 a = proactiveHapticsTrigger.getCurrentAcceleration();

        if (isPositive)
        {
            if (a.z > 0) dynamicHapticChangeRate = Mathf.Abs(dynamicHapticChangeRate);
            else if (a.z < 0) dynamicHapticChangeRate = - Mathf.Abs(dynamicHapticChangeRate);
            else dynamicHapticChangeRate = 0;
        }
        else
        {
            if (a.z > 0) dynamicHapticChangeRate = - Mathf.Abs(dynamicHapticChangeRate);
            else if (a.z < 0) dynamicHapticChangeRate = Mathf.Abs(dynamicHapticChangeRate);
            else dynamicHapticChangeRate = 0;
        }


        dynamicHapticStartA = Mathf.Max(0f, Mathf.Min(dynamicHapticStartA + dynamicHapticChangeRate, 1f));

        StartContinuesHaptic(dynamicHapticStartF, dynamicHapticStartA);
        Debug.Log("amplitude\t" + dynamicHapticStartA);
    }

    private void StopHaptic()
    {
        if (rightHand) OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.RTouch);
        if (leftHand) OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.LTouch);
        resetDynamicHaptic();
    }

    private void swingFandA()
    {
        if (dynamicHapticStartF > 1f)
        {
            dynamicHapticStartF = 1f;
            dynamicHapticChangeRate = -Mathf.Abs(dynamicHapticChangeRate);
        }
        else if (dynamicHapticStartF < 0f)
        {
            dynamicHapticStartF = 0f;
            dynamicHapticChangeRate = Mathf.Abs(dynamicHapticChangeRate);
        }

        if (dynamicHapticStartA > 1f)
        {
            dynamicHapticStartA = 1f;
            dynamicHapticChangeRate = -Mathf.Abs(dynamicHapticChangeRate);
        }
        else if (dynamicHapticStartA < 0f)
        {
            dynamicHapticStartA = 0f;
            dynamicHapticChangeRate = Mathf.Abs(dynamicHapticChangeRate);
        }
    }
}
