/*
Authors: Yihao Dong, Pamuditha Somarathne, Nisal Menuka Gamage, Anusha Withana
Contact: yihao.dong@sydney.edu.au, psom0870@uni.sydney.edu.au, nkan8027@uni.sydney.edu.au, anusha.withana@sydney.edu.au
Created: 12 Dec 2024

Description:
This script provide the hand trajectories prediction based on Gamage et al.
    ProactiveHapticTrigger script uses this script for updating object/trigger position.
    MathNet.Numerics package is reuqred for matrix calculation, which can be installed using NuGet for Unity

License:
Creative Commons Attribution 4.0 International Public License

2024 AidLAB 
The University of Sydney
*/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MathNet.Numerics.LinearAlgebra; // install the pakage using NuGet for Unity
using MathNet.Numerics.LinearAlgebra.Single;

public class HandPredictionModelMatrix : MonoBehaviour
{
    private Vector3[] positionHistory;
    private Vector3 currentPosition;
    private int sampleSize;
    private float sampleTimeInterval; // Sampling Time interval
    private float predictionTimeInterval; // Prediction Time interval
    private float[] AlphaVector;
    private Matrix<float> InversedMatrix;

    public HandPredictionModelMatrix(int sampleSize, float sampleTimeInterval, float predictionTimeInterval)
    {
        this.sampleSize = sampleSize;
        this.sampleTimeInterval = sampleTimeInterval;
        this.predictionTimeInterval = predictionTimeInterval;
        AlphaVector = CalculateAlphaVector(predictionTimeInterval);
        InversedMatrix = genaratePesudoInversMatrix(sampleSize, sampleTimeInterval);
        positionHistory = new Vector3[sampleSize];
    }

    public Vector3 CalculateDisplacement()
    {
        //Stopwatch stopwatch = Stopwatch.StartNew(); // check the device proformance

        Matrix<float> termsMatrix = CalculateVTerms(); // 3 by 5

        Vector3 v = new(termsMatrix[0, 0], termsMatrix[1, 0], termsMatrix[2, 0]);
        Vector3 a = new(termsMatrix[0, 1], termsMatrix[1, 1], termsMatrix[2, 1]);
        Vector3 j = new(termsMatrix[0, 2], termsMatrix[1, 2], termsMatrix[2, 2]);
        Vector3 s = new(termsMatrix[0, 3], termsMatrix[1, 3], termsMatrix[2, 3]);
        Vector3 c = new(termsMatrix[0, 4], termsMatrix[1, 4], termsMatrix[2, 4]);

        // scale with time interval
        Vector3 vTerm = v * predictionTimeInterval;
        Vector3 aTerm = a * (float)Math.Pow(predictionTimeInterval, 2);
        Vector3 jTerm = j * (float)Math.Pow(predictionTimeInterval, 3);
        Vector3 sTerm = s * (float)Math.Pow(predictionTimeInterval, 4);
        Vector3 cTerm = c * (float)Math.Pow(predictionTimeInterval, 5);

        // dot product with the AlphaVector
        float D_x = vTerm.x * AlphaVector[0] +
                aTerm.x * AlphaVector[1] +
                jTerm.x * AlphaVector[2] +
                sTerm.x * AlphaVector[3] +
                cTerm.x * AlphaVector[4];

        float D_y = vTerm.y * AlphaVector[0] +
                aTerm.y * AlphaVector[1] +
                jTerm.y * AlphaVector[2] +
                sTerm.y * AlphaVector[3] +
                cTerm.y * AlphaVector[4];

        float D_z = vTerm.z * AlphaVector[0] +
                aTerm.z * AlphaVector[1] +
                jTerm.z * AlphaVector[2] +
                sTerm.z * AlphaVector[3] +
                cTerm.z * AlphaVector[4];

        //stopwatch.Stop();
        //UnityEngine.Debug.Log($"Elapsed time: {stopwatch.ElapsedMilliseconds} ms");
        //UnityEngine.Debug.Log($"Elapsed time (high precision): {stopwatch.Elapsed.TotalMilliseconds} ms");

        return new Vector3(D_x, D_y, D_z);

    }

    public float[] CalculateAlphaVector(float t) // only for t < 160ms based on the original model from Gamage et al.
    {
        AlphaVector = new float[5] {
            1 + 0.1693F * t,
            0.5F + 0.1837F * t,
            0.1667F + 0.1151F * t,
            0.0417F - 0.0343F * t,
            0.0083F - 0.0064F * t
        };

        return AlphaVector;
    }

    public void RecordPosition(Vector3 newPosition) // the logic is different than the simple version
    {
        Vector3[] newPositionHistory = new Vector3[sampleSize];
        newPositionHistory[0] = this.currentPosition; // insert P_1 - the old currentPosition - to index 0
        for (int i = 1; i < newPositionHistory.Length; i++) // start from index 1
        {
            newPositionHistory[i] = this.positionHistory[i - 1]; // the old index - 1, discard the oldest
        }
        this.positionHistory = newPositionHistory; // updates
        this.currentPosition = newPosition;
    }

    Matrix<float> CalculateVTerms() // 5 by 3 -> x y z for velocities acceleration, jerk, snap, and crackle
    {
        Matrix<float> deltaPositions = Matrix<float>.Build.Dense(3, this.sampleSize, 0); // 3 by n

        for (int i = 0; i < sampleSize; i++) // fill the delta matrix P_t - P_0
        {
            deltaPositions[0, i] = this.positionHistory[i].x - this.currentPosition.x;
            deltaPositions[1, i] = this.positionHistory[i].y - this.currentPosition.y;
            deltaPositions[2, i] = this.positionHistory[i].z - this.currentPosition.z;
        }

        Matrix<float> resultMatrix = deltaPositions * InversedMatrix; // 3 by n * n by 5

        return resultMatrix; // 3 by 5
    }

    private static Matrix<float> genaratePesudoInversMatrix(int numSamplePoints, float sampleTimeInterval)
    {

        Matrix<float> matrix = Matrix<float>.Build.Dense(5, numSamplePoints, 0); // 5 by n

        for (int i = 0; i < 5; i++)
        {
            for (int j = 1; j < numSamplePoints; j++)
            {
                matrix[i, j] = -1 * (float)Math.Pow((j + 1) * sampleTimeInterval, (i + 1)) * (1 / Factorial(i + 1));
            }
        }

        return matrix.PseudoInverse(); // n by 5
    }

    private static float Factorial(int n)
    {
        if (n == 0) return 1;
        float result = 1;
        for (int i = 1; i <= n; i++)
        {
            result *= i;
        }
        return result;
    }

    public float DisplacementEquation(float v, float a, float j, float s, float c, float t, float d)
    {
        return v * t + 0.1693f * v * (t * t) + 0.5f * a * (t * t) + 0.1837f * a * (t * t * t) + 0.1667f * j * (t * t * t) + 0.1151f * j * (t * t * t * t) + 0.0417f * s * (t * t * t * t) - 0.0343f * s * (t * t * t * t * t) + 0.0083f * c * (t * t * t * t * t) - 0.0064f * c * (t * t * t * t * t * t) - d;
    }

    public float VelocityEquation(Vector3 v, Vector3 a, Vector3 j, Vector3 s, Vector3 c, float t)
    {
        Vector3 v_pred = v + 0.3386f * v * t + a * t + 0.5511f * a * (t * t) + 0.5f * j * (t * t) + 0.4604f * j * (t * t * t) + 0.1668f * s * (t * t * t) - 0.1715f * s * (t * t * t * t) + 0.0415f * c * (t * t * t * t) - 0.0192f * c * (t * t * t * t * t);
        return v_pred.magnitude;
    }

    public float FindVelocityRoot(Vector3 v, Vector3 a, Vector3 j, Vector3 s, Vector3 c)
    {
        float t_l = 0.0f, t_r = 0.128f;
        if (VelocityEquation(v, a, j, s, c, t_l) * VelocityEquation(v, a, j, s, c, t_r) > 0)
        {
            return -1f;
        }
        for (int i = 0; i < 8; i++)
        {
            float t_m = (t_l + t_r) / 2;
            if (VelocityEquation(v, a, j, s, c, t_m) > 0)
            {
                t_r = t_m;
            }
            else
            {
                t_l = t_m;
            }
        }
        return (t_l + t_r) / 2;
    }

    public float FindDisplacementRoot(Vector3 v, Vector3 a, Vector3 j, Vector3 s, Vector3 c, float d)
    {
        float t_l = 0.0f, t_r = 0.128f;
        if (DisplacementEquation(v.z, a.z, j.z, s.z, c.z, t_l, d) * DisplacementEquation(v.z, a.z, j.z, s.z, c.z, t_r, d) > 0)
        {
            return -1f;
        }
        for (int i = 0; i < 8; i++)
        {
            float t_m = (t_l + t_r) / 2;
            if (DisplacementEquation(v.z, a.z, j.z, s.z, c.z, t_m, d) > 0)
            {
                t_r = t_m;
            }
            else
            {
                t_l = t_m;
            }
        }
        return (t_l + t_r) / 2;
    }

    public float[] CalculateOvershootTime(Vector3 v, Vector3 a, Vector3 j, Vector3 s, Vector3 c, float d)
    {
        float[] result = new float[2];
        result[0] = FindVelocityRoot(v, a, j, s, c);
        result[1] = FindDisplacementRoot(v, a, j, s, c, d);
        result[0] = result[0] - result[1];
        return result;
    }
}
