using UnityEngine;
using Vuforia;
using System.Collections;
using System;
using System.IO;
using System.Linq;

[RequireComponent(typeof(Camera))]
public class NoiseCalibration : MonoBehaviour 
{
	[SerializeField] Shader m_noiseCalibrationShader;
	[SerializeField] Shader m_calcVariationShader;
	[SerializeField] Shader m_unlitTextureShader;

	[SerializeField] DrawHistogram m_drawHistogramRef;

	private const Image.PIXEL_FORMAT m_PixelFormat = Image.PIXEL_FORMAT.RGBA8888;
	private const int COLOR_DEPTH = 256;
	private const int PROC_SUBTEX_SIZE = 64;
	private const int NUM_REF_FRAMES = 5;

	private enum NoiseCalibrationStep
	{
		CALIBRATION_WAIT = 0,
		REF_IMG_GRAB = 1,
		CONSTR_HISTOGRAM = 2,
		CALIBRATION_FINISHED = 3,
	};
	private NoiseCalibrationStep m_currentStep;
	private NoiseCalibrationStep m_prevStep;

	private Material m_calcAvgMat;
	private Material m_calcVarMat;
	private Material m_unlitTexMat;

	private Texture2D[] m_refFrames;
	private RenderTexture[] m_varFrames;
	private RenderTexture m_avgFrame;

	private int m_refFrameCounter;
	private bool m_secureCoroutine = true;
	private int[,] m_histogram;

	//private Texture2D DEBUG_TEX_0;
	
	void Start () 
	{
		//DEBUG_TEX_0 = new Texture2D(PROC_SUBTEX_SIZE, PROC_SUBTEX_SIZE, TextureFormat.RGB24, false);

		m_calcAvgMat = new Material(m_noiseCalibrationShader);
		m_calcVarMat = new Material(m_calcVariationShader);
		m_unlitTexMat = new Material(m_unlitTextureShader);

		m_refFrames = new Texture2D[NUM_REF_FRAMES];
		m_varFrames = new RenderTexture[NUM_REF_FRAMES];

		for (int i = 0; i < NUM_REF_FRAMES; i++) {
			m_refFrames[i] = new Texture2D(PROC_SUBTEX_SIZE, PROC_SUBTEX_SIZE, TextureFormat.RGB24, false);
			m_varFrames[i] = new RenderTexture(PROC_SUBTEX_SIZE, PROC_SUBTEX_SIZE, 0, RenderTextureFormat.ARGB32);
		}
		m_avgFrame = new RenderTexture (PROC_SUBTEX_SIZE, PROC_SUBTEX_SIZE, 0, RenderTextureFormat.ARGB32);
		m_histogram = new int[3, 512];

		m_refFrameCounter = 0;
		m_currentStep = NoiseCalibrationStep.CALIBRATION_WAIT;
		m_prevStep = NoiseCalibrationStep.CONSTR_HISTOGRAM;
		StartCoroutine (WaitForCalibration());
	}
	
	private void OnPostRender()
	{
		// Config steps
		switch (m_currentStep) {
		case NoiseCalibrationStep.CALIBRATION_WAIT: return;
		case NoiseCalibrationStep.REF_IMG_GRAB: GrabReferenceFrames(); return;
		case NoiseCalibrationStep.CONSTR_HISTOGRAM: CalculateHistogram (); return;
		case NoiseCalibrationStep.CALIBRATION_FINISHED: break;
		default: return;
		}

		// Draw debug textures over Vuforia camera image
		StartCoroutine (FinalRender ());
	}

	private void CalculateHistogram()
	{
		GetComponent<Camera> ().targetTexture = null;

		// Calculate average texture
		m_calcAvgMat.SetTexture("_Texture_Frame_0", m_refFrames[0]);
		m_calcAvgMat.SetTexture("_Texture_Frame_1", m_refFrames[1]);
		m_calcAvgMat.SetTexture("_Texture_Frame_2", m_refFrames[2]);
		m_calcAvgMat.SetTexture("_Texture_Frame_3", m_refFrames[3]);
		m_calcAvgMat.SetTexture("_Texture_Frame_4", m_refFrames[4]);
		m_calcAvgMat.SetFloat("_NumRefFrames", NUM_REF_FRAMES);
		Graphics.Blit(null, m_avgFrame, m_calcAvgMat);

		// Calculate variaton texture
		m_calcVarMat.SetTexture ("_Texture_Avg_Frame", m_avgFrame);
		for (int i = 0; i < NUM_REF_FRAMES; i++) {
			m_calcVarMat.SetTexture ("_Texture_Ref_Frame", m_refFrames[i]);
			Graphics.Blit (null, m_varFrames[i], m_calcVarMat);
		}

		// Generate Histogram
		for (int i = 0; i < NUM_REF_FRAMES; i++) 
		{
			Texture2D tex = new Texture2D (PROC_SUBTEX_SIZE, PROC_SUBTEX_SIZE, TextureFormat.RGB24, false);
			RenderTexture.active = m_varFrames[i];
			tex.ReadPixels(new Rect(0, 0, PROC_SUBTEX_SIZE, PROC_SUBTEX_SIZE), 0, 0);
			tex.Apply ();
			byte[] bytes = tex.GetRawTextureData();

			for (int j = 0; j < bytes.Length; j+=3) 
			{
				m_histogram [0, Convert.ToInt32(bytes[j])]++;
				m_histogram [1, Convert.ToInt32(bytes[j+1])]++;
				m_histogram [2, Convert.ToInt32(bytes[j+2])]++;
			}
			RenderTexture.active = null;
		}

		// Finished
		m_currentStep = NoiseCalibrationStep.CALIBRATION_FINISHED;
		//m_drawHistogramRef.PassHistogramData(m_histogram);
		//StartCoroutine (PrintHistogram ());
	}

	private void PrintStep()
	{
		if (m_currentStep != m_prevStep) {
			m_prevStep = m_currentStep;
			Debug.Log (m_currentStep);
		}
	}

	private void GrabReferenceFrames()
	{
		RenderTexture.active = null;
		m_refFrames[m_refFrameCounter].ReadPixels(new Rect(
			Screen.width/2 - PROC_SUBTEX_SIZE/2,
			Screen.height/2 - PROC_SUBTEX_SIZE/2, 
			PROC_SUBTEX_SIZE, PROC_SUBTEX_SIZE), 0, 0);
		m_refFrames[m_refFrameCounter].Apply();

		++m_refFrameCounter;
		m_refFrameCounter %= NUM_REF_FRAMES;

		if (m_refFrameCounter <= 0)
			m_currentStep = NoiseCalibrationStep.CONSTR_HISTOGRAM;
	}

	private IEnumerator PrintHistogram()
	{
		yield return new WaitForSeconds(0.1f);
		Debug.Log ("HISTOGRAM OUTPUT:");
		for (int j = 0; j <= m_histogram.GetUpperBound(1); j++) 
			Debug.Log(j + ": [" + m_histogram [0, j] + ", " + m_histogram [1, j] + ", " + m_histogram [2, j] + "];");
	}

	private IEnumerator WaitForCalibration()
	{
		yield return new WaitForSeconds(2.0f);
		m_currentStep = NoiseCalibrationStep.REF_IMG_GRAB;
	}

	private IEnumerator FinalRender()
	{
		yield return new WaitForEndOfFrame();

		int SIZE = Convert.ToInt32(PROC_SUBTEX_SIZE * 1.0f);
		Graphics.DrawTexture(new Rect(0, 0, SIZE, SIZE), m_avgFrame);
		for (int i = 0; i < NUM_REF_FRAMES; i++) 
		{
			Graphics.DrawTexture(new Rect(SIZE * i, SIZE, SIZE, SIZE), m_refFrames [i]);
			Graphics.DrawTexture(new Rect(SIZE * i, SIZE * 2, SIZE, SIZE), m_varFrames[i]);
		}
		//Graphics.DrawTexture(new Rect(0, SIZE*3, SIZE, SIZE), DEBUG_TEX_0);
	}
}
