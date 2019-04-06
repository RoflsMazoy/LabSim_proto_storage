using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GoogleARCore;
using System;
using System.Runtime.InteropServices;

public class PointClouder : MonoBehaviour
{

    public TextMesh logger;
    public GameObject nodeShape;
    public GameObject voxelParent;
    public Text captureMessage;
    public Camera arCamera;
    public ARCoreBackgroundRenderer background;
    public ARCoreSession ARSessionManager;

    private OctTree tree;

    private bool AddVoxels = false;

    // Start is called before the first frame update
    void Start()
    {
        tree = new OctTree(nodeShape);
        ARSessionManager.RegisterChooseCameraConfigurationCallback(ChooseCameraConfiguration);
        ARSessionManager.enabled = true;
    }

    private int ChooseCameraConfiguration(List<CameraConfig>supportedConfigurations)
    {
        return 0;
    }

    public void ToggleCapture()
    {
        AddVoxels = !AddVoxels;
        if (background != null)
        {
            background.enabled = AddVoxels;
        }

        if (AddVoxels)
        {
            captureMessage.text = "Display capture";
        }
        else
        {
            captureMessage.text = "Enable Capture";
        }

    }

    private Color YUV2RGB (byte y, byte u, byte v)
    {
        return new Color(Mathf.Clamp((1.16f * (y - 16) + 1.596f * (v - 128)) / 255.0f, 0.0f, 1.0f),
                         Mathf.Clamp((1.164f * (y - 16) - 0.813f * (v - 128) - 0.391f * (u - 128)) / 255.0f, 0.0f, 1.0f),
                         Mathf.Clamp((1.164f * (y - 16) + 2.018f * (u - 128)) / 255.0f, 0.0f, 1.0f));
    }

    private Color GetColourAt(CameraImageBytes cim, int cx, int cy, out bool foundColour)
    {
        Color colour = new Color(0, 0, 0);
        foundColour = false;
        
        if ((cx >= 0) && (cy >= 0) && (cx < cim.Width) && (cy < cim.Height))
        {
            byte[] buf = new byte[1];
            byte y;
            byte u;
            byte v;

            Marshal.Copy(new IntPtr(cim.Y.ToInt64() + cy * cim.YRowStride + cx * 1), buf, 0, 1);
            y = buf[0];
            //UV planes are at quarter resolution, make sure to keep in mind when editing values
            Marshal.Copy(new IntPtr(cim.U.ToInt64() + (cy / 2) * cim.UVRowStride + (cx / 2) * cim.UVPixelStride), buf, 0, 1);
            u = buf[0];
            Marshal.Copy(new IntPtr(cim.V.ToInt64() + (cy / 2) * cim.UVRowStride + (cx / 2) * cim.UVPixelStride), buf, 0, 1);
            v = buf[0];
            colour = YUV2RGB(y, u, v);
            foundColour = true;
        }
        return colour;
    }

    // Update is called once per frame
    void Update()
    {
     if (Frame.PointCloud.IsUpdatedThisFrame)
        {
            if (logger != null)
            {
                logger.text = "Have Points";
            }
            if (AddVoxels)
            {
                CameraImageBytes cim = Frame.CameraImage.AcquireCameraImageBytes();

                for (int i = 0; i < Frame.PointCloud.PointCount; i++)
                {
                    Color colour = new Color(0, 0, 0);
                    bool foundColour = false;

                    PointCloudPoint p = Frame.PointCloud.GetPointAsStruct(i);
                    Vector3 cameraCoordinates = arCamera.WorldToViewportPoint(p);

                    if (cim.IsAvailable)
                    {
                        var uvQuad = Frame.CameraImage.DisplayUvCoords;
                        int cx = (int)(cameraCoordinates.x * cim.Width);
                        int cy = (int)((1.0f - cameraCoordinates.y) * cim.Height);
                        colour = GetColourAt(cim, cx, cy, out foundColour);
                    }
                    if (foundColour)
                    {
                        tree.addPoint(p, colour);
                    }
                }
                cim.Release();
            }
            tree.renderOctTree(voxelParent);
        }
     else
        {
            if (logger != null)
            {
                logger.text = "No Points";
            }
        }
    }
}
