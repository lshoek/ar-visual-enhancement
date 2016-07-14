using UnityEngine;
using Vuforia;
using System.Collections;
using System;

[RequireComponent (typeof(Camera))]
public class CustomVideoBackground : MonoBehaviour 
{
	/** NOISE CONFIG **/
	[SerializeField] Texture2D DefaultNoiseTexture;
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

		Shader.DisableKeyword ("IOSBUILD_OFF");
		Shader.DisableKeyword ("IOSBUILD_IPADAIR1");
		Shader.DisableKeyword ("IOSBUILD_IPADAIR2");

#if UNITY_IOS
		/** Determine the device to use their corresponding shaders **/
		switch (UnityEngine.iOS.Device.generation) 
		{
		case UnityEngine.iOS.DeviceGeneration.iPadAir1:
			#if !UNITY_EDITOR
			Debug.Log ("Running outside Unity Editor. iOS-specific shader modifications activated.");
			Shader.EnableKeyword ("IOSBUILD_IPADAIR1");
			#endif
			break;

		case UnityEngine.iOS.DeviceGeneration.iPadAir2:
			#if !UNITY_EDITOR
			Debug.Log ("Running outside Unity Editor. iOS-specific shader modifications activated.");
			Shader.EnableKeyword ("IOSBUILD_IPADAIR2");
			#endif
			break;

		default:
			Shader.EnableKeyword ("IOSBUILD_OFF");
			break;
		}
#else
		Shader.EnableKeyword ("IOSBUILD_OFF");
#endif
		Debug.Log ("IOSBUILD_OFF:" + Shader.IsKeywordEnabled("IOSBUILD_OFF"));
		Debug.Log ("IOSBUILD_IPADAIR1:" + Shader.IsKeywordEnabled("IOSBUILD_IPADAIR1"));
		Debug.Log ("IOSBUILD_IPADAIR2:" + Shader.IsKeywordEnabled("IOSBUILD_IPADAIR2"));
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

		m_noiseDelayCounter %= NOISE_DELAY_FRAMES;

		/*** TEST CODE BEGIN
		Vector2 scale = new Vector2 (1.0f, 1.0f);
#if UNITY_EDITOR
		scale.x += Input.mousePosition.x / Screen.width;
		scale.y += Input.mousePosition.y / Screen.height;
#else
		if (Input.touches.Length > 0) {
			scale.x += Input.GetTouch(0).position.x / Screen.width;
			scale.y += Input.GetTouch(0).position.y / Screen.height;
		}
#endif
		m_renderTarget.GetComponent<Renderer> ().material.SetVector ("_TexScale", scale);
		Debug.Log ("X:" + scale.x + ", Y:" + scale.y);
		/*** TEST CODE END ***/
	}
}
