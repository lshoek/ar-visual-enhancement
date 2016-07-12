using UnityEngine;
using System.Collections;

[RequireComponent (typeof(Camera), typeof(Renderer))]
public class PostProcessFX : MonoBehaviour 
{
    [SerializeField] Camera SrcCam;
	[SerializeField] Shader PPFXShader;

    private Material m_PPFXMat;

	void Start () {
		m_PPFXMat = new Material (PPFXShader);
        GetComponent<Renderer>().material = m_PPFXMat;
	}
	
	void Update () {
		float horizontal = 1.0f;
		float texSize = (horizontal > 0) ? SrcCam.targetTexture.width : SrcCam.targetTexture.height;

        m_PPFXMat.SetFloat("_ElapsedTime", Time.time);
		m_PPFXMat.SetFloat("_Horizontal", 1.0f);
		m_PPFXMat.SetFloat("_BlurSize", 1.0f/texSize);
	}

    void OnPostRender() {
        Graphics.Blit(SrcCam.targetTexture, null, m_PPFXMat);
    }
}