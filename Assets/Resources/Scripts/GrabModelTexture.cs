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

	private const int MOTIONBLUR_SIZE = 4; // Has to be the same as defined in SimpleEdgeBlur shader
	private const float WAIT_SECONDS = 3.0f;

	private Vector3 m_currBlurPos;
	private Vector3 m_prevBlurPos;
	private Vector3 m_blurVec;

	private RenderTexture m_modelTexture;
	private int m_camResWidth;
	private int m_camResHeight;

	private void Start () 
	{
		StartCoroutine (WaitForVuforiaCamData (WAIT_SECONDS));
		m_camResWidth = 640;
		m_camResHeight = 640;
		m_currBlurPos = new Vector3(1.0f, 0, 0);
		m_prevBlurPos = new Vector3(0, 1.0f, 0);
		m_customMat = new Material (m_customShader);
		m_modelTexture = new RenderTexture (640, 640, 24, RenderTextureFormat.ARGB32);
		m_modelTexture.antiAliasing = 2;
		GetComponent<Camera> ().targetTexture = m_modelTexture;
	}

	private void OnPostRender() 
	{
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
		Graphics.Blit (GetComponent<Camera>().targetTexture, m_modelTexture, m_customMat);
		Debug.Log (m_blurVec.magnitude);
	}

	private IEnumerator WaitForVuforiaCamData(float s)
	{
		yield return new WaitForSeconds(s);
		m_camResWidth = CameraDevice.Instance.GetVideoMode (CameraDevice.CameraDeviceMode.MODE_OPTIMIZE_QUALITY).width;
		m_camResHeight = CameraDevice.Instance.GetVideoMode (CameraDevice.CameraDeviceMode.MODE_OPTIMIZE_QUALITY).height;
		m_modelTexture = new RenderTexture (m_camResWidth, m_camResHeight, 24, RenderTextureFormat.ARGB32);

		Debug.Log ("VUFURIA WH: " + m_camResWidth + " x " + m_camResHeight + ", " + CameraDevice.Instance.IsActive());
	}

	public RenderTexture GetModelTexture()
	{
		return m_modelTexture;
	}
}
