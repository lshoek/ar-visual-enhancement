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

	private const Image.PIXEL_FORMAT m_PixelFormat = Image.PIXEL_FORMAT.RGBA8888;
	private const int COLOR_DEPTH = 256;
	private const int PROC_SUBTEX_SIZE = 64;
	private const int NUM_REF_FRAMES = 4;

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

	[SerializeField] Texture2D[] m_refFrames; // PROBLEM!!! ALL FRAMES ARE THE SAME
	private RenderTexture[] m_varFrames;
	private RenderTexture m_avgFrame;

	private bool m_secureCoroutine = true;
	private int[,] m_histogram;

	[SerializeField] RenderTexture DEBUG_TEX_1;
	[SerializeField] RenderTexture DEBUG_TEX_2;
	
	void Start () 
	{
		DEBUG_TEX_1 = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.Default);
		DEBUG_TEX_2 = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.Default);

		m_calcAvgMat = new Material(m_noiseCalibrationShader);
		m_calcVarMat = new Material(m_calcVariationShader);
		m_unlitTexMat = new Material(m_unlitTextureShader);

		m_refFrames = new Texture2D[NUM_REF_FRAMES];
		m_varFrames = new RenderTexture[NUM_REF_FRAMES];

		for (int i = 0; i < NUM_REF_FRAMES; i++) {
			m_refFrames[i] = new Texture2D(PROC_SUBTEX_SIZE, PROC_SUBTEX_SIZE, TextureFormat.RGB24, false);
			m_varFrames[i] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.Default);
		}
		m_avgFrame = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.Default);
		m_histogram = new int[3, 512];

		m_currentStep = NoiseCalibrationStep.CALIBRATION_WAIT;
		m_prevStep = NoiseCalibrationStep.CONSTR_HISTOGRAM;
		StartCoroutine (WaitForCalibration());
	}
	
	private void OnPostRender()
	{
		// Config steps
		switch (m_currentStep) {
		case NoiseCalibrationStep.CALIBRATION_WAIT: return;
		case NoiseCalibrationStep.REF_IMG_GRAB: 
			if (m_secureCoroutine) StartCoroutine (GrabReferenceFrames ());return;
		case NoiseCalibrationStep.CONSTR_HISTOGRAM: CalculateHistogram (); return;
		case NoiseCalibrationStep.CALIBRATION_FINISHED: break;
		default: return;
		}

		StartCoroutine (FinalRender ());
//		Graphics.Blit (m_avgFrame, DEBUG_TEX_1, m_unlitTexMat);
//		Graphics.Blit (m_varFrames[0], DEBUG_TEX_2, m_unlitTexMat);
	}

	private void CalculateHistogram()
	{
		GetComponent<Camera> ().targetTexture = null;

		// Calculate average texture
		m_calcAvgMat.SetTexture("_Texture_Frame_0", m_refFrames[0]);
		m_calcAvgMat.SetTexture("_Texture_Frame_1", m_refFrames[1]);
		m_calcAvgMat.SetTexture("_Texture_Frame_2", m_refFrames[2]);
		m_calcAvgMat.SetTexture("_Texture_Frame_3", m_refFrames[3]);
		m_calcAvgMat.SetFloat("_NumRefFrames", NUM_REF_FRAMES);
		Graphics.Blit(null, m_avgFrame, m_calcAvgMat);

		// Calculate variaton texture
		m_calcVarMat.SetTexture ("_Texture_Avg_Frame", m_avgFrame);
		for (int i = 0; i < NUM_REF_FRAMES; i++) {
			m_calcVarMat.SetTexture ("_Texture_Ref_Frame", m_refFrames[i]);
			Graphics.Blit (null, m_varFrames[i], m_calcVarMat);
		}

		// Create subtextures
//		Texture2D[] texData = new Texture2D[NUM_REF_FRAMES];
//		for (int i = 0; i < NUM_REF_FRAMES; i++)
//		{
//			texData[i] = new Texture2D (PROC_SUBTEX_SIZE, PROC_SUBTEX_SIZE, TextureFormat.RGB24, false);
//			RenderTexture.active = m_varFrames[i];
//			texData[i].ReadPixels( new Rect(PROC_SUBTEX_SIZE, PROC_SUBTEX_SIZE, 
//				Screen.width/2 - PROC_SUBTEX_SIZE/2, 
//				Screen.height/2 - PROC_SUBTEX_SIZE/2), 0, 0);
//			texData[i].Apply();
//			RenderTexture.active = null;
//		}

		// Generate Histogram
		for (int i = 0; i < NUM_REF_FRAMES; i++) 
		{
			byte[] tex = m_refFrames[i].GetRawTextureData();
			for (int j = 0; j < tex.Length/3; j++) 
			{
				m_histogram [0, Convert.ToInt32(tex[j*3]) + COLOR_DEPTH]++;
				m_histogram [1, Convert.ToInt32(tex[j*3+1]) + COLOR_DEPTH]++;
				m_histogram [2, Convert.ToInt32(tex[j*3+2]) + COLOR_DEPTH]++;
			}
		}

		// Finished
		m_currentStep = NoiseCalibrationStep.CALIBRATION_FINISHED;
		StartCoroutine (PrintHistogram ());
	}

	private void PrintStep()
	{
		if (m_currentStep != m_prevStep) {
			m_prevStep = m_currentStep;
			Debug.Log (m_currentStep);
		}
	}

	private IEnumerator GrabReferenceFrames()
	{
		m_secureCoroutine = false;
		Debug.Log("Grabbing...");
 		for (int i = 0; i < NUM_REF_FRAMES; i++) 
		{
			m_refFrames[i].ReadPixels(new Rect(PROC_SUBTEX_SIZE, PROC_SUBTEX_SIZE, 
				Screen.width/2 - PROC_SUBTEX_SIZE/2,
				Screen.height/2 - PROC_SUBTEX_SIZE/2), 0, 0);
			m_refFrames[i].Apply();
			yield return null;
		}
		m_currentStep = NoiseCalibrationStep.CONSTR_HISTOGRAM;
		Debug.Log("...Done!");
	}

	private IEnumerator PrintHistogram()
	{
		yield return new WaitForSeconds(1.0f);
		Debug.Log ("HISTOGRAM OUTPUT:");
		for (int j = 0; j < COLOR_DEPTH*2; j++) 
			Debug.Log(j + " " + " R: " + m_histogram [0, j] + " G: " + m_histogram [1, j] + " B: " + m_histogram [2, j]);
	}

	private IEnumerator WaitForCalibration()
	{
		yield return new WaitForSeconds(3.0f);
		m_currentStep = NoiseCalibrationStep.REF_IMG_GRAB;
	}

	private IEnumerator FinalRender()
	{
		yield return new WaitForEndOfFrame();
		Graphics.DrawTexture(new Rect(0, 0, PROC_SUBTEX_SIZE, PROC_SUBTEX_SIZE), m_varFrames[1]);
	}
}
