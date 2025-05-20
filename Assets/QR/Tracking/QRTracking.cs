using Meta.XR;
using NUnit.Framework;
using PassthroughCameraSamples;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZXing;
using UnityEngine.Assertions;

public class QRTracking : MonoBehaviour
{
    [SerializeField] private WebCamTextureManager webCamTextureManager;
    [SerializeField] private PassthroughCameraPermissions cameraPermissions;
    [SerializeField] private TMP_Text uiText;
    [SerializeField] private RawImage uiImage;
    [SerializeField] private GameObject prefab;
    [SerializeField] private EnvironmentRayCastSampleManager environmentRayCastSampleManager;

    private WebCamTexture texture;
    private GameObject markerPrefab;
    private Vector3[] cornerPoints = new Vector3[3];

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cameraPermissions.AskCameraPermissions();
        markerPrefab = Instantiate(prefab, Vector3.zero, Quaternion.identity);
        markerPrefab.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (webCamTextureManager.WebCamTexture == null)
        {

            if (uiText == null) return;
            if (PassthroughCameraPermissions.HasCameraPermission == false) uiText.text = "Camera permission not granted";
            else uiText.text = "Texture is null";
            return;
        }

        texture = webCamTextureManager.WebCamTexture;
        if (uiImage != null) uiImage.texture = texture;

        var barcodeReader = new BarcodeReaderGeneric { AutoRotate = false, Options = new ZXing.Common.DecodingOptions { TryHarder = false } };
        var luminanceSource = new Color32LuminanceSource(texture.GetPixels32(), texture.width, texture.height);
        var result = barcodeReader.Decode(luminanceSource);
        if (result != null)
        {
            if (uiText != null) uiText.text = $"QR Code detected: {result.Text}";

            Vector3[] positions = new Vector3[result.ResultPoints.Length];
            Vector3[] normals = new Vector3[result.ResultPoints.Length];
            for (int i = 0; i < result.ResultPoints.Length; i++)
            {
                var point = result.ResultPoints[i];
                var ray = PassthroughCameraUtils.ScreenPointToRayInWorld(webCamTextureManager.Eye, new Vector2Int((int)point.X, (int)point.Y));
                (Vector3? position, Vector3? normal) = environmentRayCastSampleManager.PlaceGameObjectByScreenPosAndRot(ray);
                UnityEngine.Assertions.Assert.IsTrue(position != null, $"Position is null for point {i}");
                UnityEngine.Assertions.Assert.IsTrue(normal != null, $"Normal is null for point {i}");
                positions[i] = position.Value;
                normals[i] = normal.Value;
            }

            // set the position of the prefab to the center of the points
            Vector3 center = Vector3.zero;
            for (int i = 0; i < positions.Length; i++)
            {
                center += positions[i];
            }
            center /= positions.Length;
            markerPrefab.transform.position = center;

            // 1. set the normal of the markerPrefab to the normal of plane defined by the points

            cornerPoints[0] = positions[getIndexWithMinDistance(positions, positions[0])];
            cornerPoints[1] = positions[getIndexWithMinDistance(positions, positions[1])];
            cornerPoints[2] = positions[getIndexWithMinDistance(positions, positions[2])];
            Vector3 ab = cornerPoints[1] - cornerPoints[0];
            Vector3 ac = cornerPoints[2] - cornerPoints[0];
            Vector3 planeNormal = Vector3.Cross(ab, ac).normalized;
            markerPrefab.transform.rotation = Quaternion.FromToRotation(transform.up, planeNormal) * transform.rotation;

            // 2. set the normals of the prefab to the average normals of the points
            // Vector3 averageNormal = Vector3.zero;
            // for (int i = 0; i < normals.Length; i++)
            // {
            //     averageNormal += normals[i];
            // }
            // averageNormal /= normals.Length;
            // markerPrefab.transform.rotation = Quaternion.FromToRotation(transform.up, averageNormal) * transform.rotation;


            markerPrefab.SetActive(true);
        }
        else
        {
            if (uiText != null) uiText.text = "No QR Code detected";
        }
    }
    
    private int getIndexWithMinDistance(Vector3[] points, Vector3 point)
    {
        int index = 0;
        float minDistance = float.MaxValue;
        for (int i = 0; i < points.Length; i++)
        {
            float distance = Vector3.Distance(points[i], point);
            if (distance < minDistance)
            {
                minDistance = distance;
                index = i;
            }
        }
        return index;
    }   
}

 public class Color32LuminanceSource : BaseLuminanceSource

    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Color32LuminanceSource"/> class.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        public Color32LuminanceSource(int width, int height)
           : base(width, height)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Color32LuminanceSource"/> class.
        /// </summary>
        /// <param name="color32s">The color32s.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        public Color32LuminanceSource(Color32[] color32s, int width, int height)
           : base(width, height)
        {
            SetPixels(color32s);
        }

        /// <summary>
        /// Sets the pixels.
        /// </summary>
        /// <param name="color32s">The color32s.</param>
        public void SetPixels(Color32[] color32s)
        {
            var z = 0;

            for (var y = Height - 1; y >= 0; y--)
            {
                // This is flipped vertically because the Color32 array from Unity is reversed vertically,
                // it means that the top most row of the image would be the bottom most in the array.
                for (var x = 0; x < Width; x++)
                {
                    var color32 = color32s[y * Width + x];
                    // Calculate luminance cheaply, favoring green.
                    luminances[z++] = (byte)((
                       color32.r +
                       color32.g + color32.g +
                       color32.b) >> 2);
                }
            }
        }

        /// <summary>
        /// Should create a new luminance source with the right class type.
        /// The method is used in methods crop and rotate.
        /// </summary>
        /// <param name="newLuminances">The new luminances.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <returns></returns>
        protected override LuminanceSource CreateLuminanceSource(byte[] newLuminances, int width, int height)
        {
            return new Color32LuminanceSource(width, height) { luminances = newLuminances };
        }
    }
