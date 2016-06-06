using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Vuforia;

[RequireComponent (typeof (Camera))]
public class GrabModelTexture : MonoBehaviour 
{
	[SerializeField] Shader m_customShader;
	[SerializeField] RenderTexture m_modelTexture;

	[SerializeField][Range(0, 1.0f)] float m_AA_Weight = 1.0f;
	[SerializeField] bool m_enableEdgeAA = true;

	private Material m_customMat;
	private int m_camResWidth;
	private int m_camResHeight;

	private void Start () 
	{
		StartCoroutine (WaitForVuforiaCamData ());
		m_camResWidth = 640;
		m_camResHeight = 640;
		m_customMat = new Material (m_customShader);
		m_modelTexture = new RenderTexture (640, 640, 24, RenderTextureFormat.ARGB32);
		GetComponent<Camera> ().targetTexture = m_modelTexture;
	}

	private void OnPostRender() 
	{
		m_customMat.SetFloat ("_CamRes_Width", m_camResWidth);
		m_customMat.SetFloat ("_CamRes_Height", m_camResHeight);
		m_customMat.SetFloat ("_EnableEdgeAntiAliasing", (m_enableEdgeAA) ? 1.0f : 0);
		m_customMat.SetFloat ("_AA_Weight", m_AA_Weight);
		Graphics.Blit (GetComponent<Camera>().targetTexture, m_modelTexture, m_customMat);
	}

	private IEnumerator WaitForVuforiaCamData()
	{
		yield return new WaitForSeconds(4.0f);
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
