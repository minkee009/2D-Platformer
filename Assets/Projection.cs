using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Projection : MonoBehaviour
{
    public Vector3[] Boxes = new Vector3[8];

    public float sh = 16;
    public float sw = 9;

    public float fovAngle = 60;

    public float zfar = 1000f;
    public float znear = 0.01f;

    public float fovFactor = 1.0f;

    public Transform mainCam;

    private void OnValidate()
    {
        mainCam = Camera.main.transform;
    }

    private void OnDrawGizmos()
    {
        var aspect = sh / sw;
        var frustum = 1 / Mathf.Tan(fovAngle * 0.5f);
        var projectedPoint = new Vector3[8];
        var count = 0;
        //var q = zfar / (zfar - znear);
        //var qr = (-zfar * znear) / (zfar - znear);
        foreach (var point in Boxes)
        {
            projectedPoint[count] = new Vector3(point.x, point.y, 0f) + new Vector3(transform.position.x, transform.position.y, 0f);

            projectedPoint[count].x *= aspect * frustum;
            projectedPoint[count].y *= frustum;

            var z = (point.z + transform.position.z);
            if (z != 0.0f)
            {
                projectedPoint[count].x /= z;
                projectedPoint[count].y /= z;
            }

            count++;
        }

        for (int i = 0; i < 4; i++)
        {
            Gizmos.DrawLine(projectedPoint[i], projectedPoint[(i + 1) % 4]);
        }
        for (int i = 4; i < 8; i++)
        {
            Gizmos.DrawLine(projectedPoint[i], projectedPoint[(i + 1) % 8 == 0 ? 4 : (i + 1)]);
        }

        for (int i = 0; i < 4; i++)
        {
            Gizmos.DrawLine(projectedPoint[i], projectedPoint[i + 4]);
        }
    }
}
