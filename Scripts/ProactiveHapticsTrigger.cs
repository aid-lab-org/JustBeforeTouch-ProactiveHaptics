
/*
Authors: Yihao Dong, Pamuditha Somarathne, Nisal Menuka Gamage, Anusha Withana
Contact: yihao.dong@sydney.edu.au, psom0870@uni.sydney.edu.au, nkan8027@uni.sydney.edu.au, anusha.withana@sydney.edu.au
Created: 12 Dec 2024

Description:
This script updates the GameObject's position every frame using the predicted position 
    provided by HandPredictionModelMatrix.

Usage:
Add this script to the GameObject which triggers the haptics.

Parameters:
SAMPLE_SIZE: Number of history samples used for each prediction.
SAMPLE_INTERVAL: The prediction frequency, in seconds. This changes Time.fixedDeltaTime. 
    The SAMPLE_INTERVAL should match the headset refresh rate for the best result. 
PREDICTION_TIME: Defines how far into the future (in seconds) the attached GameObject 
    should be positioned ahead of the trackingOrigin.
trackingOrigin: The GameObject which provides the current position. 
predictedPosition: The predicted position for the current frame, can be used for logging.

License:
Creative Commons Attribution 4.0 International Public License

2024 AidLAB 
The University of Sydney
*/

SAMPLE_SIZE: Number of history samples used for each prediction.

SAMPLE_INTERVAL: The prediction frequency, in seconds. This changes Time.fixedDeltaTime. 
The SAMPLE_INTERVAL should match the headset refresh rate for the best result. 

PREDICTION_TIME: Defines how far into the future (in seconds) the attached GameObject 
should be positioned ahead of the trackingOrigin.

trackingOrigin: The GameObject which provides the current position. 

predictedPosition: The predicted position for the current frame, can be used for logging. 

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;

public class ProactiveHapticsTrigger : MonoBehaviour
{
    public int SAMPLE_SIZE = 50;
    public float SAMPLE_INTERVAL = 0.0125F;
    public float PREDICION_TIME = 0.1F;


    private HandPredictionModelMatrix predictionModel;

    public GameObject trackingOrigin;

    public Vector3 predictedPotision { get; private set; }

    // Start is called before the first frame update
    void Start()
    {
        Time.fixedDeltaTime = SAMPLE_INTERVAL; // call fixedUpdate every xxx ms
        predictionModel = new HandPredictionModelMatrix(SAMPLE_SIZE, SAMPLE_INTERVAL, PREDICION_TIME);
    }

    // Update is called once per frame
    void Update()
    {
        // update the position of the attached GameObject of the script
        transform.position = predictedPotision;
    }

    private void FixedUpdate()
    {
        Vector3 initPosition = GetTrackingOriginPosition();

        predictionModel.RecordPosition(initPosition);
        predictedPotision = initPosition + predictionModel.CalculateDisplacement();
    }

    public Vector3 GetTrackingOriginPosition()
    {
        return trackingOrigin.transform.position;
    }
}
