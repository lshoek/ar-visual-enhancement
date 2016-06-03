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

	private Material m_customMat;
	private int m_texSizePow2;

	private int m_vuCamResWidth;
	private int m_vuCamResHeight;

	private void Start () 
	{
		m_vuCamResWidth = Screen.width;
		m_vuCamResHeight = Screen.height;
		StartCoroutine (WaitForVuforiaCamData ());

		int size = (Screen.width > Screen.height) ? Screen.width : Screen.height;
		m_texSizePow2 = GetNearestPowerOf2 (size);
		Debug.Log ("SCREENSIZE: " + size + " TEXSIZE: " + m_texSizePow2);

		m_customMat = new Material (m_customShader);
		m_modelTexture = new RenderTexture (m_texSizePow2, m_texSizePow2, 24, RenderTextureFormat.ARGB32);
		GetComponent<Camera> ().targetTexture = m_modelTexture;
	}

	private void OnPostRender() 
	{
		Graphics.Blit (GetComponent<Camera>().targetTexture, m_modelTexture, m_customMat);
	}

	private int GetNearestPowerOf2(int n)
	{
		int[] pows = { 0, 128, 256, 512, 1024, 2048, 4096 };

		for (int i = 0; i < pows.Length; i++)
			if (n > pows[i] && n < pows[i+1])
				return pows[i+1];

		Debug.Log("GrabModelTexture: Screen texture is too large! " + " n=" + n);
		return -1;
	}

	private IEnumerator WaitForVuforiaCamData()
	{
		yield return new WaitForSeconds(3.0f);
		m_vuCamResWidth = CameraDevice.Instance.GetVideoMode (CameraDevice.CameraDeviceMode.MODE_OPTIMIZE_QUALITY).width;
		m_vuCamResHeight = CameraDevice.Instance.GetVideoMode (CameraDevice.CameraDeviceMode.MODE_OPTIMIZE_QUALITY).height;
		Debug.Log ("VUFURIA WH: " + m_vuCamResWidth + " x " + m_vuCamResWidth + ", " + CameraDevice.Instance.IsActive());

		m_modelTexture = new RenderTexture (m_vuCamResWidth, m_vuCamResWidth, 24, RenderTextureFormat.ARGB32);
		m_modelTexture.filterMode = FilterMode.Point;
	}

	public RenderTexture GetModelTexture()
	{
		return m_modelTexture;
	}
}
