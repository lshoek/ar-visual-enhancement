using UnityEngine;
using Vuforia;
using System.Collections;
using System;
using System.IO;
using System.Linq;

[RequireComponent(typeof(Camera))]
public class NoiseDistribution : MonoBehaviour 
{
	[SerializeField] Shader m_calcAverageShader;
	[SerializeField] Shader m_calcVariationShader;
	[SerializeField] Shader m_generateNoiseTexShader;
	[SerializeField] Shader m_unlitTextureShader;

	[SerializeField] DrawHistogram m_drawHistogramRef;
	
	private const int COLOR_DEPTH = 256;
	private const int NOISE_TEX_SIZE = 128;
	private const int PROC_SUBTEX_SIZE = 128;
	private const int NUM_REF_FRAMES = 5;
	private const int NUM_NOISE_DELAY_FRAMES = 8;

	private enum NoiseDistributionStep
	{
		PROCESSING_WAIT = 0,
		REF_IMG_GRAB = 1,
		CALC_NOISE_DISTRIB = 2,
		GENERATE_NOISE_TEX = 3,
		PROCESS_FINISHED = 4
	};
	private NoiseDistributionStep m_currentStep;
	private NoiseDistributionStep m_prevStep;

	private Material m_calcAvgMat;
	private Material m_calcVarMat;
	private Material m_genNoiseTexMat;
	private Material m_unlitTexMat;

	private Texture2D[] m_refFrames;
	private RenderTexture[] m_varFrames;
	private RenderTexture m_avgFrame;
	private Texture2D m_noiseTexture;

	private int m_refFrameCounter;
	private bool m_secureCoroutine = true;
	private int[,] m_histogram;

	private double[] means = new double[3];
	private double[] sdeviations = new double[3];
	//private Texture2D DEBUG_TEX_0;
	
	void Start () 
	{
		//DEBUG_TEX_0 = new Texture2D(NOISE_TEX_SIZE, NOISE_TEX_SIZE, TextureFormat.RGB24, false);
		m_calcAvgMat = new Material(m_calcAverageShader);
		m_calcVarMat = new Material(m_calcVariationShader);
		m_genNoiseTexMat = new Material(m_generateNoiseTexShader);
		m_unlitTexMat = new Material(m_unlitTextureShader);

		m_refFrames = new Texture2D[NUM_REF_FRAMES];
		m_varFrames = new RenderTexture[NUM_REF_FRAMES];

		for (int i = 0; i < NUM_REF_FRAMES; i++) {
			m_refFrames[i] = new Texture2D(PROC_SUBTEX_SIZE, PROC_SUBTEX_SIZE, TextureFormat.RGB24, false);
			m_varFrames[i] = new RenderTexture(PROC_SUBTEX_SIZE, PROC_SUBTEX_SIZE, 0, RenderTextureFormat.ARGB32);
		}
		m_noiseTexture = new Texture2D(NOISE_TEX_SIZE, NOISE_TEX_SIZE, TextureFormat.RGB24, false);
		m_avgFrame = new RenderTexture (PROC_SUBTEX_SIZE, PROC_SUBTEX_SIZE, 0, RenderTextureFormat.ARGB32);
		m_histogram = new int[3, COLOR_DEPTH*2];

		m_refFrameCounter = 0;
		m_currentStep = NoiseDistributionStep.PROCESSING_WAIT;
		m_prevStep = NoiseDistributionStep.CALC_NOISE_DISTRIB;
		StartCoroutine (WaitForProcessing());
	}
	
	private void OnPostRender()
	{
		switch (m_currentStep) {
		case NoiseDistributionStep.PROCESS_FINISHED: break;			
		case NoiseDistributionStep.PROCESSING_WAIT: return;
		case NoiseDistributionStep.REF_IMG_GRAB: GrabReferenceFrames(); return;
		case NoiseDistributionStep.CALC_NOISE_DISTRIB: CalcNoiseDistribution (); return;
		case NoiseDistributionStep.GENERATE_NOISE_TEX: GenerateNoiseTexture (); return;
		default: return;
		}
	}

	private void CalcNoiseDistribution()
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
		//m_drawHistogramRef.PassHistogramData(m_histogram);
		//StartCoroutine (PrintHistogram ());

		// Calculate noise distribution parameters
		int numPixels = PROC_SUBTEX_SIZE*PROC_SUBTEX_SIZE*NUM_REF_FRAMES;
		double[] sums = new double[3];

		// Means
		for (int i = 0; i < 3; i++)
			for (int j = 0; j < COLOR_DEPTH; j++)
				sums [i] += Convert.ToDouble (m_histogram [i, j]) * ((j - COLOR_DEPTH / 2.0)+1.0);
				//Debug.Log(j + " : " + ((j - COLOR_DEPTH / 2.0)+1.0) + " * " + Convert.ToDouble (m_histogram [i, j]));

		// Divide by pixel total, reuse sums[]
		for (int i = 0; i < 3; i++) {
			means [i] = sums [i] / numPixels;
			sums [i] = 0;
		}

		// Standard deviations
		for (int i = 0; i < 3; i++)
			for (int j = 0; j < COLOR_DEPTH; j++)
				sums[i] += Convert.ToDouble(m_histogram [i, j]) * Math.Pow(((j - COLOR_DEPTH / 2.0)+1.0) - means[i], 2.0);
				//Debug.Log("sd: " + Convert.ToDouble(m_histogram [i, j]) + " * Math.Pow(" + ((j - COLOR_DEPTH / 2.0)+1.0) + " - " + means[i] + ", 2.0);");

		// Divide by pixel total
		for (int i = 0; i < 3; i++)
			sdeviations [i] = sums [i] / numPixels;

		// Finished
		PrintNoiseDistributionResults();
		m_currentStep = NoiseDistributionStep.GENERATE_NOISE_TEX;
	}

	private void GenerateNoiseTexture ()
	{
		//GenNoiseTextureGPU();
		byte GRAY = 127;
		byte[] bytes = m_noiseTexture.GetRawTextureData();
		System.Random rnd = new System.Random ();

		for (int y = 0; y < NOISE_TEX_SIZE; y++) {
			for (int x = 0; x < NOISE_TEX_SIZE; x++) {
				for (int i = 0; i < 3; i++) {
					double n = GetRandomNoiseValue(rnd, means[i], sdeviations[i]);

//					double v = 1.0 / (sdeviations [i] * (2.0 * Math.PI)) * 
//						Math.Exp (-(Math.Pow (rnd.NextDouble () - means [i], 2.0) /
//						Math.Pow (2.0 * sdeviations [i], 2.0)));

					bool b = (n < 0) ? true : false;
					byte result = (b) ?
						(byte)(GRAY - Convert.ToByte(Math.Round(Math.Abs(n)))) :
						(byte)(GRAY + Convert.ToByte(Math.Round(n)));
					
					bytes[(y * NOISE_TEX_SIZE + x) * 3 + i] = result;
				}
			}
		}
		m_noiseTexture.LoadRawTextureData (bytes);
		m_noiseTexture.Apply ();
		m_currentStep = NoiseDistributionStep.PROCESS_FINISHED;
	}

	// Not used for the time being
	private void GenNoiseTextureGPU()
	{
		m_genNoiseTexMat.SetFloat("_Tex_Size", NOISE_TEX_SIZE);
		m_genNoiseTexMat.SetFloat("_Mean_R", Convert.ToSingle(means[0]));
		m_genNoiseTexMat.SetFloat("_Mean_G", Convert.ToSingle(means[1]));
		m_genNoiseTexMat.SetFloat("_Mean_B", Convert.ToSingle(means[2]));
		m_genNoiseTexMat.SetFloat("_SD_R", Convert.ToSingle(sdeviations[0]));
		m_genNoiseTexMat.SetFloat("_SD_G", Convert.ToSingle(sdeviations[1]));
		m_genNoiseTexMat.SetFloat("_SD_B", Convert.ToSingle(sdeviations[2]));
		//Graphics.Blit(m_noiseTextures[m_noiseIndex], DEBUG_TEX_0, m_genNoiseTexMat);
	}

	private double GetRandomNoiseValue(System.Random rand, double mean, double sdev)
	{
		double u1 = rand.NextDouble();
		double u2 = rand.NextDouble();
		double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
		return mean + sdev * randStdNormal;
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
			m_currentStep = NoiseDistributionStep.CALC_NOISE_DISTRIB;
	}

	public Texture2D GetNoiseTexture()
	{
		if (m_currentStep != NoiseDistributionStep.PROCESS_FINISHED)
			return null; //new Texture2D (NOISE_TEX_SIZE, NOISE_TEX_SIZE);

		return m_noiseTexture;
	}

	#region DEBUG FUNCTIONS
	private void PrintNoiseDistributionResults()
	{
		Debug.Log ("MEANS; R: " + means[0] + ", G: " + means[1] + ", B: " + means[2]);
		Debug.Log ("SDEVIATIONS; R: " + sdeviations[0] + ", G: " + sdeviations[1] + ", B: " + sdeviations[2]);
	}
	
	private void PrintStep()
	{
		if (m_currentStep != m_prevStep) {
			m_prevStep = m_currentStep;
			Debug.Log (m_currentStep);
		}
	}

	private IEnumerator PrintHistogram()
	{
		yield return new WaitForSeconds(0.1f);
		Debug.Log ("HISTOGRAM OUTPUT:");
		for (int j = 0; j <= m_histogram.GetUpperBound(1); j++) 
			Debug.Log(j + ": [" + m_histogram [0, j] + ", " + m_histogram [1, j] + ", " + m_histogram [2, j] + "];");
	}

	private IEnumerator WaitForProcessing()
	{
		yield return new WaitForSeconds(3.0f);
		m_currentStep = NoiseDistributionStep.REF_IMG_GRAB;
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
		Graphics.DrawTexture(new Rect(10.0f, SIZE*3+10.0f, SIZE*3.5f, SIZE*3.5f), m_noiseTexture);
	}
	#endregion
}
