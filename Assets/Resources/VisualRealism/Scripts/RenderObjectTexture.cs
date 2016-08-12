using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Vuforia;

[RequireComponent (typeof (Camera))]
public class RenderObjectTexture : MonoBehaviour 
{
	enum EFFECTS_CONFIG { 
		NONE, 
		MOTION_BLUR, 
		GAUSSIAN_BLUR3X3, 
		FULL 
	};
	[SerializeField] EFFECTS_CONFIG EffectsConfig;

	/** MOTION BLUR CONFIG **/
	[SerializeField][Range(1, 25)] int BLUR_SAMPLES = 11;
	[SerializeField][Range(0.25f, 5.0f)] float BLUR_RANGE = 1.0f;
	[SerializeField][Range(-10.0f, 10.0f)] float BLUR_OFFSET = -0.5f;
	[SerializeField][Range(0f, 2.0f)] float BLUR_MUL_ALPHA = 1.0f;

	[SerializeField] private Shader MotionBlurShader;
	[SerializeField] private Shader GaussianBlurShader;
	[SerializeField] private GameObject ModelRef;

	private Material MotionBlurMat;
	private Material GaussianBlurMat;
	
	private const int DEFAULT_TEX_SIZE = 1024;

	[HideInInspector] public RenderTexture ObjectTexture;
	private RenderTexture m_tempTexture;

	private Vector3 m_currBlurPos = new Vector3(1.0f, 0, 0);
	private Vector3 m_prevBlurPos = new Vector3(0, 1.0f, 0);
	private Vector3 m_blurVec;
	
	public static int CamResWidth { get; private set; }
    public static int CamResHeight { get; private set; }

	private bool m_vuforiaStarted = false;

	private void Start () 
	{
		/** Set up some default textures until the camera resolution is determined **/
        CamResWidth = DEFAULT_TEX_SIZE;
        CamResHeight = DEFAULT_TEX_SIZE;

        ObjectTexture = new RenderTexture(CamResWidth, CamResHeight, 0, RenderTextureFormat.ARGB32);
        m_tempTexture = new RenderTexture(CamResWidth, CamResHeight, 0, RenderTextureFormat.ARGB32);
        GetComponent<Camera>().targetTexture = new RenderTexture(CamResWidth, CamResHeight, 0, RenderTextureFormat.ARGB32);
		GetComponent<Camera> ().targetTexture.depth = 24;

		MotionBlurMat = new Material (MotionBlurShader);
		GaussianBlurMat = new Material (GaussianBlurShader);
	}

	private void OnPostRender() 
	{
		if (!m_vuforiaStarted)
			CheckVuforiaInitStatus();

		/** Set up the Object shaders **/
		m_prevBlurPos = m_currBlurPos;
		m_currBlurPos = GetComponent<Camera> ().WorldToScreenPoint(ModelRef.transform.position);
		m_currBlurPos.x /= Screen.width;
		m_currBlurPos.y /= Screen.height;

		if (!m_currBlurPos.Equals(m_prevBlurPos))
			m_blurVec = m_currBlurPos - m_prevBlurPos;

		MotionBlurMat.SetFloat ("_BLUR_SAMPLES", BLUR_SAMPLES);
		MotionBlurMat.SetFloat ("_BLUR_RANGE", BLUR_RANGE);
		MotionBlurMat.SetFloat ("_BLUR_OFFSET", BLUR_OFFSET);
		MotionBlurMat.SetFloat ("_BLUR_MUL_ALPHA", BLUR_MUL_ALPHA);
		MotionBlurMat.SetVector ("_MotionBlurVec", m_blurVec);
		MotionBlurMat.SetFloat ("_MotionBlurVecLength", m_blurVec.magnitude);
        GaussianBlurMat.SetFloat("_CamRes_Width", CamResWidth);
        GaussianBlurMat.SetFloat("_CamRes_Height", CamResHeight);
		GaussianBlurMat.SetFloat ("_MotionBlurVecLength", m_blurVec.magnitude);

		/** Rendering commands **/
		switch (EffectsConfig) {
		case EFFECTS_CONFIG.NONE:
			Graphics.Blit (GetComponent<Camera>().targetTexture, ObjectTexture);
			break;
		case EFFECTS_CONFIG.MOTION_BLUR:
			Graphics.Blit (GetComponent<Camera>().targetTexture, ObjectTexture, MotionBlurMat);
			break;
		case EFFECTS_CONFIG.GAUSSIAN_BLUR3X3:
			Graphics.Blit (GetComponent<Camera>().targetTexture, ObjectTexture, GaussianBlurMat);
			break;
		case EFFECTS_CONFIG.FULL:
			Graphics.Blit (GetComponent<Camera>().targetTexture, m_tempTexture, MotionBlurMat);
			Graphics.Blit (m_tempTexture, ObjectTexture, GaussianBlurMat);
			break;
		}
	}

	private void CheckVuforiaInitStatus()
	{
		try {
            CamResWidth = VuforiaRenderer.Instance.GetVideoTextureInfo().imageSize.x;
            CamResHeight = VuforiaRenderer.Instance.GetVideoTextureInfo().imageSize.y;
		} catch (System.NullReferenceException) {}

        if (CamResWidth == 0 || CamResWidth == null) 
            return;

        if (CamResWidth > Screen.width)
        {
            CamResWidth = Screen.width;
            CamResHeight = Screen.height;
        }

        ObjectTexture = new RenderTexture(CamResWidth, CamResHeight, 0, RenderTextureFormat.ARGB32);
        m_tempTexture = new RenderTexture(CamResWidth, CamResHeight, 0, RenderTextureFormat.ARGB32);
        GetComponent<Camera>().targetTexture = new RenderTexture(CamResWidth, CamResHeight, 0, RenderTextureFormat.ARGB32);
		GetComponent<Camera> ().targetTexture.depth = 24;

#if UNITY_EDITOR
        GetComponent<Camera>().targetTexture.antiAliasing = 4;
#endif

        Debug.Log("Vuforia data; w:" + CamResWidth + " x " + CamResHeight);
		m_vuforiaStarted = true;
	}

	private IEnumerator WaitRender() 
	{
		yield return new WaitForEndOfFrame();
		Graphics.DrawTexture (new Rect(0, 0, Screen.width/2.0f, Screen.height/2.0f), ObjectTexture);
	}
}
