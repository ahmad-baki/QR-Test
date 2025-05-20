using PassthroughCameraSamples;
using UnityEngine;
using ZXing;
using ZXing.Common;

public class QRTracking : MonoBehaviour
{
    public WebCamTextureManager webCamTextureManager;
    private WebCamTexture texture;
    private PassthroughCameraPermissions cameraPermissions;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Find the WebCamTextureManager in the scene
        cameraPermissions = FindFirstObjectByType<PassthroughCameraPermissions>();
        if (cameraPermissions == null)
        {
            Debug.LogError("PassthroughCameraPermissions not found in the scene.");
            return;
        }
        cameraPermissions.AskCameraPermissions(); 
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
