using UnityEngine;
using Vuforia;
using System.Collections;
using System;

[RequireComponent (typeof(Camera))]
public class CustomVideoBackground : MonoBehaviour 
{
	[SerializeField] Camera m_FXCamera;
	[SerializeField] GameObject m_renderTarget;
	[SerializeField] Texture2D m_defaultNoiseTexture;
	[SerializeField] int m_numNoiseDelayFrames = 2;

	private const int DEFAULT_NOISE_TEX_SIZE = 64;

	private int m_noiseTexSize;
	private int m_noiseDelayCounter = 0;
	private bool m_noiseTexGenerated = false;
	private bool m_noiseGenerationEnabled = false;

	private System.Random m_rng; 

	private void Start() 
	{

		m_noiseTexSize = DEFAULT_NOISE_TEX_SIZE;
		m_rng = new System.Random ();

		if (GetComponent<NoiseDistribution>() == null)
			m_noiseGenerationEnabled = false;
		else
			m_noiseGenerationEnabled = GetComponent<NoiseDistribution>().enabled;

		Debug.Log("NoiseGeneration: " + (m_noiseGenerationEnabled ? "ON" : "OFF"));
		m_defaultNoiseTexture.filterMode = FilterMode.Bilinear;
		Shader.DisableKeyword ("IOSBUILD_OFF");
		Shader.DisableKeyword ("IOSBUILD_IPADAIR1");
		Shader.DisableKeyword ("IOSBUILD_IPADAIR2");

#if UNITY_IOS
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
		Material m = m_renderTarget.GetComponent<Renderer>().material;

		if (m_noiseGenerationEnabled) {
			if (!m_noiseTexGenerated) {
				m_noiseTexGenerated = (GetComponent<NoiseDistribution> ().GetNoiseTexture() != null) ? true : false;
				m.SetTexture ("_NoiseTex", (m_noiseTexGenerated) ?
					GetComponent<NoiseDistribution> ().GetNoiseTexture() : m_defaultNoiseTexture);
				m_noiseTexSize = (m_noiseTexGenerated) ? 
					GetComponent<NoiseDistribution> ().GetNoiseTexture ().width : m_defaultNoiseTexture.width;
			}
		} else {
			m.SetTexture ("_NoiseTex", m_defaultNoiseTexture);
		}
		m.SetFloat("_NoiseTexSize", m_noiseTexSize);

		++m_noiseDelayCounter;
		if (m_noiseDelayCounter >= m_numNoiseDelayFrames) {
			m.SetFloat ("_NoiseTexOffset0", Convert.ToSingle (m_rng.Next (m_noiseTexSize)) / m_noiseTexSize);
			m.SetFloat ("_NoiseTexOffset1", Convert.ToSingle (m_rng.Next (m_noiseTexSize)) / m_noiseTexSize);
		}

		m.SetTexture("_ObjectTex", m_FXCamera.GetComponent<GrabModelTexture> ().ObjectTexture);

		m.SetFloat("_ScreenRes_Width", Screen.width);
		m.SetFloat("_ScreenRes_Height", Screen.height);

		m.SetFloat("_AspectRatio", Camera.main.aspect);
		m.SetFloat("_Vuforia_Aspect", m_renderTarget.transform.localScale.x/m_renderTarget.transform.localScale.z);

		m_noiseDelayCounter %= m_numNoiseDelayFrames;

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
		DestTarget.GetComponent<Renderer> ().material.SetVector ("_TexScale", scale);
		Debug.Log ("X:" + scale.x + ", Y:" + scale.y);
		/*** TEST CODE END ***/
	}
}
