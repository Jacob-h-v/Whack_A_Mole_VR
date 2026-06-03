using System;
using System.Collections;
using System.IO;
using UnityEngine;

public class ScreenShot : MonoBehaviour
{
    [SerializeField]
    private string screenName;

    [SerializeField]
    private Camera vrCamera; // Assign your VR Rig's camera here in the Inspector

    [SerializeField]
    private int captureWidth = 7680; // 8K resolution width

    [SerializeField]
    private int captureHeight = 4320; // 8K resolution height

    [SerializeField]
    private int rows = 4; // Number of horizontal splits

    [SerializeField]
    private int columns = 4; // Number of vertical splits

    private bool isCapturing = false;

    // Start is called before the first frame update
    public void Start()
    {
    }

    // Update is called once per frame
    public void Update()
    {
        //Press W to take a Screen Capture
        if (Input.GetKeyDown(KeyCode.W) && !isCapturing)
        {
            StartCoroutine(CaptureTiledScreenshot());
        }
    }

    private IEnumerator CaptureTiledScreenshot()
    {
        isCapturing = true;

        // Use the assigned VR camera, or fallback to Camera.main
        Camera cam = vrCamera != null ? vrCamera : Camera.main;
        
        if (cam == null)
        {
            Debug.LogError("Target Camera not found. Please assign it in the Inspector.");
            isCapturing = false;
            yield break;
        }

        int tileWidth = captureWidth / columns;
        int tileHeight = captureHeight / rows;

        Texture2D finalScreenshot = new Texture2D(captureWidth, captureHeight, TextureFormat.RGB24, false);
        RenderTexture renderTexture = new RenderTexture(tileWidth, tileHeight, 24);
        
        Matrix4x4 originalProjectionMatrix = cam.projectionMatrix;

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                // Calculate offset coordinates for each tile's rendering bounds
                float left = (float)x / columns;
                float right = (float)(x + 1) / columns;
                float bottom = (float)y / rows;
                float top = (float)(y + 1) / rows;

                cam.projectionMatrix = GetTileProjectionMatrix(cam, left, right, bottom, top);
                cam.targetTexture = renderTexture;
                cam.Render();

                RenderTexture.active = renderTexture;

                Texture2D tileTexture = new Texture2D(tileWidth, tileHeight, TextureFormat.RGB24, false);
                tileTexture.ReadPixels(new Rect(0, 0, tileWidth, tileHeight), 0, 0);
                tileTexture.Apply();

                // Yield to briefly allow runtime components/processes to catch up over intensive captures
                yield return null;

                // Stitch the current tile into the final large screenshot texture
                finalScreenshot.SetPixels(x * tileWidth, y * tileHeight, tileWidth, tileHeight, tileTexture.GetPixels());
                
                Destroy(tileTexture);
            }
        }

        finalScreenshot.Apply();

        // Encode and save the stitched screenshot
        byte[] bytes = finalScreenshot.EncodeToPNG();
        string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), screenName + ".png");
        File.WriteAllBytes(filePath, bytes);
        
        Debug.Log("Screenshot Captured and saved to: " + filePath);

        // Restore everything to their initial state
        cam.targetTexture = null;
        cam.projectionMatrix = originalProjectionMatrix;
        RenderTexture.active = null;

        Destroy(renderTexture);
        Destroy(finalScreenshot);

        isCapturing = false;
    }

    private Matrix4x4 GetTileProjectionMatrix(Camera cam, float left, float right, float bottom, float top)
    {
        float near = cam.nearClipPlane;
        float far = cam.farClipPlane;
        float fov = cam.fieldOfView;
        
        // Ensure aspect ratio is strictly bound by the final composed footprint dimensions rather than screen dimensions 
        float aspect = (float)captureWidth / captureHeight;

        float angle = fov * Mathf.Deg2Rad / 2.0f;
        float currentTop = near * Mathf.Tan(angle);
        float currentBottom = -currentTop;
        float currentRight = currentTop * aspect;
        float currentLeft = -currentRight;

        float currentWidth = currentRight - currentLeft;
        float currentHeight = currentTop - currentBottom;

        float newLeft = currentLeft + currentWidth * left;
        float newRight = currentLeft + currentWidth * right;
        float newBottom = currentBottom + currentHeight * bottom;
        float newTop = currentBottom + currentHeight * top;

        Matrix4x4 m = new Matrix4x4();
        m.m00 = 2.0f * near / (newRight - newLeft);
        m.m01 = 0;
        m.m02 = (newRight + newLeft) / (newRight - newLeft);
        m.m03 = 0;

        m.m10 = 0;
        m.m11 = 2.0f * near / (newTop - newBottom);
        m.m12 = (newTop + newBottom) / (newTop - newBottom);
        m.m13 = 0;

        m.m20 = 0;
        m.m21 = 0;
        m.m22 = -(far + near) / (far - near);
        m.m23 = -(2.0f * far * near) / (far - near);

        m.m30 = 0;
        m.m31 = 0;
        m.m32 = -1.0f;
        m.m33 = 0;

        return m;
    }
}
