using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

[RequireComponent (typeof (Camera))]
public class GrabModelTexture : MonoBehaviour 
{
	[SerializeField] Shader m_customShader;
	[SerializeField] RenderTexture m_modelTexture;

	private Material m_customMat;
	private int m_texSizePow2;

	private event Action PatternsFoundChanged;
	private event Action InvokeModelsInvisible;

	private void Start () 
	{
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

		Debug.Log("GrabModelTexture: Screen texture is too large!");
		return -1;
	}

	public RenderTexture GetModelTexture()
	{
		return m_modelTexture;
	}
}
