using UnityEngine;
using Vuforia;
using System.Collections;
using System;

[RequireComponent (typeof(Camera), typeof(NoiseDistribution))]
public class CustomVideoBackground : MonoBehaviour 
{
	[SerializeField] Camera m_FXCamera;
	[SerializeField] GameObject m_renderTarget;

	private const int NUM_NOISE_DELAY_FRAMES = 5;

	private int m_noiseTexSize;
	private int m_noiseDelayCounter;
	private bool m_noiseTexGenerated;

	private System.Random m_rng; 

	private void Start() 
	{
		m_noiseTexSize = 64;
		m_noiseDelayCounter = 0;
		m_noiseTexGenerated = false;
		m_rng = new System.Random ();

		Shader.DisableKeyword ("IOSBUILD_OFF");
		Shader.DisableKeyword ("IOSBUILD_IPADAIR1");
		Shader.DisableKeyword ("IOSBUILD_IPADAIR2");

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

		Debug.Log ("IOSBUILD_OFF:" + Shader.IsKeywordEnabled("IOSBUILD_OFF"));
		Debug.Log ("IOSBUILD_IPADAIR1:" + Shader.IsKeywordEnabled("IOSBUILD_IPADAIR1"));
		Debug.Log ("IOSBUILD_IPADAIR2:" + Shader.IsKeywordEnabled("IOSBUILD_IPADAIR2"));
	}

	private void OnPreRender() 
	{
		Material m = m_renderTarget.GetComponent<Renderer>().material;

		if (!m_noiseTexGenerated) {
			m_noiseTexGenerated = (GetComponent<NoiseDistribution> ().GetNoiseTexture() != null) ? true : false;
			m.SetTexture ("_NoiseTex", (m_noiseTexGenerated) ?
				GetComponent<NoiseDistribution> ().GetNoiseTexture() : new Texture2D(64, 64));
			m_noiseTexSize = (m_noiseTexGenerated) ? 
				GetComponent<NoiseDistribution> ().GetNoiseTexture ().width : m_noiseTexSize;
		}
		m.SetFloat("_NoiseTexSize", m_noiseTexSize);

		++m_noiseDelayCounter;
		if (m_noiseDelayCounter >= NUM_NOISE_DELAY_FRAMES) {
			m.SetFloat ("_NoiseTexOffset0", Convert.ToSingle (m_rng.Next (m_noiseTexSize)) / m_noiseTexSize);
			m.SetFloat ("_NoiseTexOffset1", Convert.ToSingle (m_rng.Next (m_noiseTexSize)) / m_noiseTexSize);
		}

		m.SetTexture("_ObjectTex", m_FXCamera.GetComponent<GrabModelTexture> ().GetModelTexture ());

		m.SetFloat("_ScreenRes_Width", Screen.width);
		m.SetFloat("_ScreenRes_Height", Screen.height);
		m.SetFloat("_AspectRatio", Camera.main.aspect);
		m.SetFloat("_Vuforia_Aspect", m_renderTarget.transform.localScale.x/m_renderTarget.transform.localScale.z);

		m_noiseDelayCounter %= NUM_NOISE_DELAY_FRAMES;

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
