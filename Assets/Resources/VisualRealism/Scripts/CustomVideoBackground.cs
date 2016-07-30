using UnityEngine;
using Vuforia;
using System.Collections;
using System;

[RequireComponent (typeof(Camera))]
public class CustomVideoBackground : MonoBehaviour 
{
	/** NOISE CONFIG **/
	[SerializeField] Texture2D DefaultNoiseTexture;
	[SerializeField] bool DEBUG_OBJECT_TEX = false;
	[SerializeField] bool FORCE_DEFAULT_TEX_SCALE = false;
	[SerializeField] bool ENABLE_NOISE = true;
	[SerializeField] bool ENABLE_ALPHA_MIXING = true;
	[SerializeField][Range(0, 60)] int NOISE_DELAY_FRAMES = 2;
	[SerializeField][Range(0, 100.0f)] float MULTIPLY_NOISE = 10.0f;
	[SerializeField][Range(0, 1.0f)] float INTENSITY_BIAS = 0.6f;
	[SerializeField][Range(0, 16.0f)] float TEXEL_MAGNIFICATION = 2.0f;
			
	private const int DEFAULT_NOISE_TEX_SIZE = 64;

	private Camera FXCamera;
	private GameObject RenderTarget;

	private int m_noiseTexSize;
	private int m_noiseDelayCounter = 0;
	private bool m_noiseTexGenerated = false;
	private bool m_noiseGenerationEnabled = false;
	private System.Random m_rng;

	private enum TargetDevices {
		DEFAULT,
		IPADAIR1,
		IPADAIR2,
		IPADAIRPRO_SMALL,
		ANDROID,
		DEBUG
	};
	private int m_currentTargetDevice;
	private float[,] m_deviceScales = {
		{ 1.0f, 1.0f },
		{ 1.595f, 1.420f },
		{ 1.585f, 1.420f },
		{ 1.600f, 1.067f },
		{ 1.595f, 1.420f },
		{ 1.0f, 1.0f },
	};

	private void Start() 
	{
		FXCamera = GameObject.FindWithTag("FXCamera").GetComponentInChildren<Camera>();
		RenderTarget = GameObject.FindWithTag("BackgroundPlane");

		m_noiseTexSize = DEFAULT_NOISE_TEX_SIZE;
		m_rng = new System.Random ();

		if (GetComponent<NoiseDistribution>() == null)
			m_noiseGenerationEnabled = false;
		else
			m_noiseGenerationEnabled = GetComponent<NoiseDistribution>().enabled;

		Debug.Log("NoiseGeneration: " + (m_noiseGenerationEnabled ? "ON" : "OFF"));
		DefaultNoiseTexture.filterMode = FilterMode.Bilinear;

		m_currentTargetDevice = (int)TargetDevices.DEFAULT;
		if (DEBUG_OBJECT_TEX) {
			m_currentTargetDevice = (int)TargetDevices.DEBUG;
		} else {
#if !UNITY_EDITOR
#if UNITY_ANDROID 
			Debug.Log ("Running outside Unity Editor. Android-specific shader modifications activated.");
			m_currentTargetDevice = (int)TargetDevices.ANDROID;
#elif UNITY_IOS
			switch (UnityEngine.iOS.Device.generation) 
			{
			case UnityEngine.iOS.DeviceGeneration.iPadAir1:
				Debug.Log ("m_currentTargetDevice: iPadAir1");
				m_currentTargetDevice = (int)TargetDevices.IPADAIR1;
				break;

			case UnityEngine.iOS.DeviceGeneration.iPadAir2:
				Debug.Log ("m_currentTargetDevice: iPadAir2");
				m_currentTargetDevice = (int)TargetDevices.IPADAIR2;
				break;

			case UnityEngine.iOS.DeviceGeneration.Unknown:
				Debug.Log ("m_currentTargetDevice: iPadAirPro?");
				m_currentTargetDevice = (int)TargetDevices.IPADAIRPRO_SMALL;
				break;
			}
#endif
#endif
		}
	}

	private void OnPreRender() 
	{
		/** Render FXCamera first to update ObjectTexture **/
		FXCamera.Render();

		/** Prepare the CustomVideoBackground shader **/
		Material m = RenderTarget.GetComponent<Renderer>().material;

		m.SetFloat ("_ENABLE_NOISE", ENABLE_NOISE ? 1.0f : 0);
		m.SetFloat ("_ENABLE_ALPHA_MIXING", ENABLE_ALPHA_MIXING ? 1.0f : 0);
		m.SetFloat ("_MULTIPLY_NOISE", MULTIPLY_NOISE);
		m.SetFloat ("_INTENSITY_BIAS", INTENSITY_BIAS);
		m.SetFloat ("_TEXEL_MAGNIFICATION", TEXEL_MAGNIFICATION);

		if (m_noiseGenerationEnabled) {
			if (!m_noiseTexGenerated) {
				m_noiseTexGenerated = (GetComponent<NoiseDistribution> ().GetNoiseTexture() != null) ? true : false;
				m.SetTexture ("_NoiseTex", (m_noiseTexGenerated) ?
					GetComponent<NoiseDistribution> ().GetNoiseTexture() : DefaultNoiseTexture);
				m_noiseTexSize = (m_noiseTexGenerated) ? 
					GetComponent<NoiseDistribution> ().GetNoiseTexture ().width : DefaultNoiseTexture.width;
			}
		} else {
			m.SetTexture ("_NoiseTex", DefaultNoiseTexture);
		}
		m.SetFloat("_NoiseTexSize", m_noiseTexSize);

		++m_noiseDelayCounter;
		if (m_noiseDelayCounter >= NOISE_DELAY_FRAMES) {
			m.SetFloat ("_NoiseTexOffset0", Convert.ToSingle (m_rng.Next (m_noiseTexSize)) / m_noiseTexSize);
			m.SetFloat ("_NoiseTexOffset1", Convert.ToSingle (m_rng.Next (m_noiseTexSize)) / m_noiseTexSize);
		}
		m.SetTexture("_ObjectTex", FXCamera.GetComponent<RenderObjectTexture> ().ObjectTexture);

		m.SetFloat("_ScreenRes_Width", Screen.width);
		m.SetFloat("_ScreenRes_Height", Screen.height);

		m.SetFloat("_AspectRatio", Camera.main.aspect);
		m.SetFloat("_Vuforia_Aspect", RenderTarget.transform.localScale.x/RenderTarget.transform.localScale.z);

		Vector2 scale = new Vector2 (m_deviceScales[m_currentTargetDevice, 0], m_deviceScales[m_currentTargetDevice, 1]);
		if (DEBUG_OBJECT_TEX) {
#if UNITY_EDITOR
			scale.x += Input.mousePosition.x / Screen.width;
			scale.y += Input.mousePosition.y / Screen.height;
#else
			if (Input.touches.Length > 0) {
				scale.x += Input.GetTouch(0).position.x / Screen.width;
				scale.y += Input.GetTouch(0).position.y / Screen.height;
			}
#endif
		}
		m.SetVector ("_TEX_SCALE", scale);
		string debugText = "MAGIC NUMBERS; X:" + scale.x + ", Y:" + scale.y;
		GameObject.FindGameObjectWithTag("DebugText").GetComponent<UnityEngine.UI.Text>().text = debugText;
		Debug.Log (debugText);

		m_noiseDelayCounter %= NOISE_DELAY_FRAMES;
	}
}
