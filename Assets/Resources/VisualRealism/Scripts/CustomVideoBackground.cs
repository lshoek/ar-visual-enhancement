using UnityEngine;
using Vuforia;
using System.Collections;
using System;

[RequireComponent (typeof(Camera))]
public class CustomVideoBackground : MonoBehaviour 
{
	/** NOISE CONFIG **/
    [SerializeField] Shader CustomVideoBackgroundShader;
	[SerializeField] Texture2D DefaultNoiseTexture;
	[SerializeField] bool ENABLE_NOISE = true;
	[SerializeField] bool ENABLE_ALPHA_MIXING = true;
    [SerializeField][Range(12, 60)] int CAMERA_FRAME_RATE = 30;
	[SerializeField][Range(0, 60)] int NOISE_DELAY_FRAMES = 2;
	[SerializeField][Range(0, 100.0f)] float MULTIPLY_NOISE = 10.0f;
	[SerializeField][Range(0, 1.0f)] float INTENSITY_BIAS = 0.6f;
	[SerializeField][Range(0, 16.0f)] float TEXEL_MAGNIFICATION = 2.0f;
			
	private const int DEFAULT_NOISE_TEX_SIZE = 64;

	private Camera FXCamera;

	private int m_noiseTexSize;
	private int m_noiseDelayCounter = 0;
	private bool m_noiseTexGenerated = false;
	private bool m_noiseGenerationEnabled = false;
	private System.Random m_rng;

    private void Awake()
    {
        Application.targetFrameRate = CAMERA_FRAME_RATE;
    }

	private void Start() 
	{
		FXCamera = GameObject.FindWithTag("FXCamera").GetComponentInChildren<Camera>();

		m_noiseTexSize = DEFAULT_NOISE_TEX_SIZE;
		m_rng = new System.Random ();

		if (GetComponent<NoiseDistribution>() == null)
			m_noiseGenerationEnabled = false;
		else
			m_noiseGenerationEnabled = GetComponent<NoiseDistribution>().enabled;

		Debug.Log("NoiseGeneration: " + (m_noiseGenerationEnabled ? "ON" : "OFF"));
		DefaultNoiseTexture.filterMode = FilterMode.Bilinear;
	}

	private void OnPreRender() 
	{
		/** Render FXCamera first to update ObjectTexture **/
		FXCamera.Render();
    }

    private void OnRenderImage(RenderTexture source, RenderTexture dest)
    {
		/** Prepare the CustomVideoBackground shader **/
        Material m = new Material(CustomVideoBackgroundShader);

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

		m.SetFloat("_VideoRes_Width", RenderObjectTexture.CamResWidth);
        m.SetFloat("_VideoRes_Height", RenderObjectTexture.CamResHeight);

        m.SetFloat("_Screen_Aspect", (float)Screen.width/Screen.height);

        m_noiseDelayCounter %= NOISE_DELAY_FRAMES;

        Graphics.Blit(source, dest, m);

        string debugText = "DEBUG";
        GameObject.FindWithTag("DebugText").GetComponentInChildren<UnityEngine.UI.Text>().text = debugText;
	}
}
