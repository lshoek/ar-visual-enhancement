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

	private void Start () 
	{
		StartCoroutine (WaitForVuforiaCamData ());

		int size = (Screen.width > Screen.height) ? Screen.width : Screen.height;
		m_customMat = new Material (m_customShader);
		m_modelTexture = new RenderTexture (640, 640, 24, RenderTextureFormat.ARGB32);
		GetComponent<Camera> ().targetTexture = m_modelTexture;
	}

	private void OnPostRender() 
	{
		Graphics.Blit (GetComponent<Camera>().targetTexture, m_modelTexture, m_customMat);
	}

	private IEnumerator WaitForVuforiaCamData()
	{
		yield return new WaitForSeconds(3.0f);
		int w = CameraDevice.Instance.GetVideoMode (CameraDevice.CameraDeviceMode.MODE_OPTIMIZE_QUALITY).width;
		int h = CameraDevice.Instance.GetVideoMode (CameraDevice.CameraDeviceMode.MODE_OPTIMIZE_QUALITY).height;
		m_modelTexture = new RenderTexture (w, h, 24, RenderTextureFormat.ARGB32);

		Debug.Log ("VUFURIA WH: " + w + " x " + h + ", " + CameraDevice.Instance.IsActive());
	}

	public RenderTexture GetModelTexture()
	{
		return m_modelTexture;
	}
}
