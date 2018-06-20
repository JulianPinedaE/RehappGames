﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Globalization;

public class PlayerPosition : MonoBehaviour
{
    public float speedUp;
    public float posAmplifier;

    public static float PlayerFrontalSpeed = 0;
    public static float distanceCovered = 0;

    Vector3 acc;
    Vector3 mean;
    float[] accDataX = { 0, 0, 0, 0, 0, 0};
    float[] accDataY = { 0, 0, 0, 0, 0, 0};
    float[] accDataZ = { 0, 0, 0, 0, 0, 0};
    int init = 0;
    float linearSpeed = 0;
    float lastPos = 0;
    float newPos = 0;
    float moved = 0;
    int targetLostCount = 0;
    int newTargetCount = 0;
    float targetLostTimer = 0;
    bool accAdjusted = false;

    WebCamTexture cam;
    Texture2D tex;
    Texture2D targetTex;

    byte[] camImg;
    byte[] targetImg;
    int targetCenterX;
    int targetCenterY;
    bool lostTarget = true;
    bool thereIsTarget = false;
    double matchVal;
    int matchPosX;
    int matchPosY;

    private void OnEnable()
    {
        Debug.Log(Application.persistentDataPath);
        cam = GlobalCam.gameCam;
        tex = new Texture2D(640, 480, TextureFormat.RGB24, false);
    }

    private void FixedUpdate()
    {
        acc.x = Input.acceleration.x;
        acc.y = Input.acceleration.y;
        acc.z = Input.acceleration.z;

        if (SkiingController.onGame)
        {
            distanceCovered += PlayerFrontalSpeed * Time.deltaTime;
            GetAccArray();
            GetOffset();
            if (init < 6)
                init++;
            else
            {
                PlayerHorizontalPos();
            }

            if (lostTarget)
            {
                NewTarget();
            }
            if (thereIsTarget)
            {
                TargetMatch();

                DetectLostTarget();

                SetPlayerSpeed();
            }

            //Debug.Log("Ang x: " + gravityAngle.x + "Ang y: " + gravityAngle.y + "Ang z: " + gravityAngle.z);
            //Debug.Log("Magnitud: " + Input.acceleration.magnitude);
        }
    }

    private void SetPlayerSpeed()
    {
        //Debug.Log("Match: " + matchVal);
        newPos = (float)(matchVal / 10000000);
        moved = newPos - lastPos;
        lastPos = newPos;
        linearSpeed = Math.Abs(moved);

        if (linearSpeed < 0.1)
            PlayerFrontalSpeed = 0.1f;
        else
            PlayerFrontalSpeed = linearSpeed * speedUp;

        //Debug.Log("linearSpeed: " + PlayerFrontalSpeed);
    }

    private void DetectLostTarget()
    {
        targetLostTimer += Time.deltaTime;
        if (targetLostTimer > 2)
        {
            targetLostTimer = 0;
            targetLostCount = 0;
            newTargetCount++;
            if (newTargetCount > 3)
            {
                Debug.Log("New target!");
                newTargetCount = 0;
                lostTarget = true;
                thereIsTarget = false;
            }
        }
        if (newPos > 4)
        {
            targetLostCount++;
            if (targetLostCount > 50)
            {
                newTargetCount = 0;
                targetLostCount = 0;
                Debug.Log("Lost!");
                lostTarget = true;
                thereIsTarget = false;
            }
        }
    }

    private void TargetMatch()
    {
        TakePic();

        targetImg = targetTex.GetRawTextureData();

        OcvMechanics.MatchTemplateImg(camImg, cam.width, cam.height, targetImg, targetTex.width, targetTex.height, out matchVal, out matchPosX, out matchPosY);
        //Debug.Log("Match: " + matchVal);
        Debug.Log("1");
    }

    private void NewTarget()
    {
        TakePic();

        OcvMechanics.GetTarget(camImg, cam.width, cam.height, out targetCenterX, out targetCenterY);

        CutTarget();
        lostTarget = false;
        thereIsTarget = true;

        //transform.position = new Vector3(Math.Abs(targetCenterX - cam.width), targetCenterY, 0);
    }

    private void TakePic()
    {
        tex.SetPixels32(cam.GetPixels32());
        tex.Apply();
        camImg = tex.GetRawTextureData();
    }

    private void PlayerHorizontalPos()
    {
        transform.position = new Vector3(mean.x, transform.position.y, transform.position.z)*posAmplifier;
    }

    private void CutTarget()
    {
        float minX;
        float maxX;
        float minY;
        float maxY;
        float windowSize = 10;
        if (targetCenterX < (cam.width / windowSize))
        {
            minX = 0;
            maxX = 2 * cam.width / windowSize;
        }
        else
        {
            if (targetCenterX > (cam.width - (cam.width / windowSize)))
            {
                maxX = cam.width;
                minX = cam.width - 2 * cam.width / windowSize;
            }
            else
            {
                maxX = targetCenterX + cam.width / windowSize;
                minX = targetCenterX - (cam.width / windowSize);
            }
        }
        if (targetCenterY < (cam.height / windowSize))
        {
            minY = 0;
            maxY = 2 * cam.height / windowSize;
        }
        else
        {
            if (targetCenterY > (cam.height - (cam.height / windowSize)))
            {
                maxY = cam.height;
                minY = cam.height - 2 * cam.height / windowSize;
            }
            else
            {
                maxY = targetCenterY + cam.height / windowSize;
                minY = targetCenterY - (cam.height / windowSize);
            }
        }

        targetTex = new Texture2D((int)(maxX - minX), (int)(maxY - minY), TextureFormat.RGB24, false);
        targetTex.SetPixels(0, 0, targetTex.width, targetTex.height, cam.GetPixels((int)minX, (int)minY, targetTex.width, targetTex.height));
        targetTex.Apply();

        //byte[] bytes = targetTex.EncodeToPNG();
        //File.WriteAllBytes(Application.persistentDataPath + "/Target.png", bytes);
    }

    private void GetOffset()
    {
        mean.x = accDataX.Average();
        mean.y = accDataY.Average();
        mean.z = accDataZ.Average();
        //Debug.Log("x: " + mean.x + "y: " + mean.y + "z: " + mean.z);
    }

    void GetAccArray()
    {
        Array.Copy(accDataX, 0, accDataX, 1, accDataX.Length - 1);
        accDataX[0] = acc.x;
        Array.Copy(accDataY, 0, accDataY, 1, accDataY.Length - 1);
        accDataY[0] = acc.y;
        Array.Copy(accDataZ, 0, accDataZ, 1, accDataZ.Length - 1);
        accDataZ[0] = acc.z;
        //Debug.Log("G: " + (acc.z - accMiddle));
        //Debug.Log(string.Join(" ", accDataZ.Select(x => x.ToString()).ToArray()));
    }

    public void StartAccAdjust()
    {
        accAdjusted = true;
        //lostTarget = true;
        //thereIsTarget = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Yay!");
        SkiingController.PlayerScore(1);
    }
}