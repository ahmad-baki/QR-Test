using Meta.XR;
using PassthroughCameraSamples;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZXing;

public class QRTracking : MonoBehaviour
{
    [SerializeField] private WebCamTextureManager webCamTextureManager;
    [SerializeField] private PassthroughCameraPermissions cameraPermissions;
    [SerializeField] private TMP_Text uiText;
    [SerializeField] private RawImage uiImage;
    [SerializeField] private GameObject prefab;
    [SerializeField] private EnvironmentRayCastSampleManager environmentRayCastSampleManager;

    private WebCamTexture texture;
    private GameObject[] cornerPoints = new GameObject[4];

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cameraPermissions.AskCameraPermissions(); 
        for (int i = 0; i < cornerPoints.Length; i++)
        {
            cornerPoints[i] = Instantiate(prefab, Vector3.zero, Quaternion.identity);
            cornerPoints[i].SetActive(false);
        }
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
            for (int i = 0; i < result.ResultPoints.Length; i++)
            {
                var point = result.ResultPoints[i];
                var ray = PassthroughCameraUtils.ScreenPointToRayInWorld(webCamTextureManager.Eye, new Vector2Int((int)point.X, (int)point.Y));
                Vector3? position = environmentRayCastSampleManager.PlaceGameObjectByScreenPos(ray);
                if (position != null)
                {
                    cornerPoints[i].SetActive(true);
                    cornerPoints[i].transform.SetPositionAndRotation(position.Value, Quaternion.LookRotation(ray.direction));
                }
                else
                {
                    if (uiText != null) uiText.text = $"Error with depth API, 2d Pos: {point.X}, {point.Y}";
                    cornerPoints[i].SetActive(false);
                }
            }
        }
        else
        {
            if (uiText != null) uiText.text = "No QR Code detected";
        }
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