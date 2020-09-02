using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class CameraImage :MonoBehaviour {
	private bool m_CameraAvailable;
	private WebCamTexture m_InputCamera;
	private Texture m_DefaultBackground;
	private bool m_isBussy;
	

	[SerializeField]
	private RawImage m_CameraRenderer = null;
	[SerializeField]
	private AspectRatioFitter m_fitter = null;
	[SerializeField]
	private Vector2Int m_resolution = new Vector2Int (1280,720);
	[SerializeField]
	private int m_fps = 30;
	[SerializeField]
	private ObjectDetector m_Detector = null;

	private IList<BoundingBox> m_boxOutlines;
	private float scaleFactor = 1.0f;
	private Vector2 screenScale = new Vector2 ();

	IEnumerator Start() {
		yield return Application.RequestUserAuthorization (UserAuthorization.WebCam);
		Application.targetFrameRate = 24;
		if (Application.HasUserAuthorization (UserAuthorization.WebCam)) {
			SetupBackCamera ();
		}
		GetScale ();
	}

	private void SetupBackCamera() {
		m_DefaultBackground = m_CameraRenderer.texture;
		WebCamDevice[] devices = WebCamTexture.devices;

		if (devices.Length == 0) {
			Debug.LogWarning ("No cameras detected in this device!");
			m_CameraAvailable = false;
		}

		foreach (WebCamDevice device in devices) {
			Debug.Log (device.name + "isFrontFacing: " + device.isFrontFacing);
			if (!device.isFrontFacing) {
				m_InputCamera = new WebCamTexture (device.name, m_resolution.x, m_resolution.y, m_fps);
				break;
			}
#if UNITY_EDITOR
			m_InputCamera = new WebCamTexture (device.name, m_resolution.x, m_resolution.y, m_fps);
			Debug.Log ("Running in editor, using: " + device.name);
			break;
#endif
		}

		if (m_InputCamera == null) {
			Debug.LogWarning ("Unable to find a Back Camera!");
			return;
		}

		m_InputCamera.Play ();
		m_CameraRenderer.texture = m_InputCamera;
		m_CameraAvailable = true;
	}

	private void GetScale() {
		int smallest;
		float inputSize;
		if (Screen.width < Screen.height) {
			smallest = Screen.width;
			inputSize = ObjectDetector.ImageNetSettings.imageWidth;
			screenScale.y = (Screen.height - smallest) / 2.0f;
		} else {
			smallest = Screen.height;
			inputSize = ObjectDetector.ImageNetSettings.imageHeight;
			screenScale.x = (Screen.width - smallest) / 2.0f;
		}

		scaleFactor = smallest / (float)inputSize;
	}

	void Update() {
		if (!m_CameraAvailable) {
			return;
		}

		float ratio = (float)m_InputCamera.width / (float)m_InputCamera.height;
		m_fitter.aspectRatio = ratio;

		float scaleY = m_InputCamera.videoVerticallyMirrored ? -1 : 1;
		m_CameraRenderer.rectTransform.localScale = new Vector3 (1f, scaleY, 1f);

		int orientation = -m_InputCamera.videoRotationAngle;
		m_CameraRenderer.rectTransform.localEulerAngles = new Vector3 (0, 0, orientation);

		DetectObjectInFrame ();
	}

	private void DetectObjectInFrame() {
		if (m_isBussy) {
			return;
		}

		m_isBussy = true;
		StartCoroutine (PrepareImage(res => {
			StartCoroutine (m_Detector.Detect(res, boxes=> {
				m_boxOutlines = boxes;
				Resources.UnloadUnusedAssets ();
				m_isBussy = false;
			}));
		}));

	
	}

	private IEnumerator PrepareImage(System.Action<Color32[]> callback) {
		// Corrutine that crops the image in a square
		StartCoroutine (
			TextureUtils.CropSquare(m_InputCamera,
			RectOptions.Center,
			cropedTexture => {
				// Scale image to the desired 416 px
				var scaledTexture = TextureUtils.ScaleTexture (cropedTexture, ObjectDetector.ImageNetSettings.imageWidth, ObjectDetector.ImageNetSettings.imageHeight, FilterMode.Bilinear);
				// Rotate Image 
				var rotatedTexture = TextureUtils.RotateImageMatrix (scaledTexture.GetPixels32(), ObjectDetector.ImageNetSettings.imageWidth, ObjectDetector.ImageNetSettings.imageHeight, -90);
				// Execute callback (ObjectDetector) once the image is done
				callback (rotatedTexture);
			}));
		yield return null;
	}

	// Draw boxes and labels of the detected objects
	private void OnGUI() {
		if (m_boxOutlines != null && m_boxOutlines.Any ()) {
			foreach (var outline in m_boxOutlines) {
				float x = outline.Dimensions.X * scaleFactor + screenScale.x;
				float width = outline.Dimensions.Width * scaleFactor;
				float y = outline.Dimensions.Y * scaleFactor + screenScale.y;
				float height = outline.Dimensions.Height * scaleFactor;

				GUI.Box (new Rect (x, y, width, height), outline.Label);
			}
		}
	}

}

