﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalCam : MonoBehaviour {

    public static WebCamTexture gameCam;
    static public int camWidth = 640;
    static public int camHeigth = 480;
    static public bool useRearCam = false;

    public static void SetGlobalCam () {
        var devices = WebCamTexture.devices;
        var cam = "";
        if (devices.Length > 0) cam = devices[0].name;
        for (int i = 0; i < devices.Length; i++)
        {
            if (devices[i].isFrontFacing && !useRearCam)
            {
                cam = devices[i].name;
            }
            if (!devices[i].isFrontFacing && useRearCam)
            {
                cam = devices[i].name;
            }
        }
        gameCam = new WebCamTexture(cam, camWidth, camHeigth, 30);
        gameCam.requestedFPS = 30;
        gameCam.Play();
    }

    public static Vector2 CamDimensions()
    {
        Vector2 w_h = new Vector2();
        w_h.x = gameCam.width;
        w_h.y = gameCam.height;
        return w_h;
    }

    public static void StopCam()
    {
        gameCam.Stop();
    }
}
