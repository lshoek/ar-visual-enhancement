using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Vuforia;

[RequireComponent (typeof (Camera))]
public class GrabModelTexture : MonoBehaviour 
{
	[SerializeField] Shader m_customShader;
	[SerializeField] Material m_customMat;
	[SerializeField] GameObject m_modelReference;

	[HideInInspector] public RenderTexture ObjectTexture;

	private const int DEFAULT_TEX_SIZE = 1024;

	private Vector3 m_currBlurPos = new Vector3(1.0f, 0, 0);
	private Vector3 m_prevBlurPos = new Vector3(0, 1.0f, 0);
	private Vector3 m_blurVec;
	
	private int m_camResWidth = 0;
	private int m_camResHeight = 0;

	private bool m_vuforiaStarted = false;

	private void Start () 
	{
		m_camResWidth = DEFAULT_TEX_SIZE;
		m_camResHeight = DEFAULT_TEX_SIZE;

#if UNITY_IOS && !UNITY_EDITOR
		switch (UnityEngine.iOS.Device.generation) 
		{	
		case UnityEngine.iOS.DeviceGeneration.iPadAir1:
			m_camResWidth = 1024;
			m_camResHeight = 768;
			break;
			
		case UnityEngine.iOS.DeviceGeneration.iPadAir2:
			m_camResWidth = 1024;
			m_camResHeight = 768;
			break;
			
		default: //2048x1536
			m_camResWidth = 1024;
			m_camResHeight = 768;
			break;
		}
		Debug.Log("GrabmodelTexture: iOS detected!");
#endif

		ObjectTexture = new RenderTexture (m_camResWidth, m_camResHeight, 24, RenderTextureFormat.ARGB32);
		GetComponent<Camera> ().targetTexture = new RenderTexture(m_camResWidth, m_camResHeight, 24, RenderTextureFormat.ARGB32);
		//GetComponent<Camera> ().targetTexture.antiAliasing = 2;

		m_customMat = new Material (m_customShader);
	}

	private void OnPostRender() 
	{
		if (!m_vuforiaStarted)
			CheckVuforiaInitStatus();

		m_prevBlurPos = m_currBlurPos;
		m_currBlurPos = GetComponent<Camera> ().WorldToScreenPoint(m_modelReference.transform.position);
		m_currBlurPos.x /= Screen.width;
		m_currBlurPos.y /= Screen.height;

		if (!m_currBlurPos.Equals(m_prevBlurPos))
			m_blurVec = m_currBlurPos - m_prevBlurPos;

		m_customMat.SetVector ("_MotionBlurVec", m_blurVec);
		m_customMat.SetFloat ("_MotionBlurVecLength", m_blurVec.magnitude);
		m_customMat.SetFloat ("_CamRes_Width", m_camResWidth);
		m_customMat.SetFloat ("_CamRes_Height", m_camResHeight);
			
		Graphics.Blit (GetComponent<Camera>().targetTexture, ObjectTexture, m_customMat);
	}

	private void CheckVuforiaInitStatus()
	{
#if UNITY_EDITOR
		try {
			m_camResWidth = VuforiaRenderer.Instance.GetVideoTextureInfo().imageSize.x;
			m_camResHeight = VuforiaRenderer.Instance.GetVideoTextureInfo().imageSize.y;
		} catch (System.NullReferenceException) {}

		if (m_camResWidth == 0)
			return;

		ObjectTexture = new RenderTexture (m_camResWidth, m_camResHeight, 24, RenderTextureFormat.ARGB32);
		GetComponent<Camera> ().targetTexture = new RenderTexture(m_camResWidth, m_camResHeight, 24, RenderTextureFormat.ARGB32);
		//GetComponent<Camera> ().targetTexture.antiAliasing = 2;

		Debug.Log ("Vuforia data; w:" + m_camResWidth + " x " + m_camResHeight);
#endif
		m_vuforiaStarted = true;
	}
}
