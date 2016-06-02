using UnityEngine;
using System.Collections;

[RequireComponent (typeof(Camera), typeof(NoiseDistribution))]
public class CustomVideoBackground : MonoBehaviour 
{
	[SerializeField] Camera m_FXCamera;
	[SerializeField] GameObject m_renderTarget;

	private void Start() 
	{
		Shader.DisableKeyword ("IOSBUILD_OFF");
		Shader.DisableKeyword ("IOSBUILD_IPADAIR1");
		Shader.DisableKeyword ("IOSBUILD_IPADAIR2");

		switch (UnityEngine.iOS.Device.generation) 
		{
		case UnityEngine.iOS.DeviceGeneration.iPadAir1:
			#if !UNITY_EDITOR
			Debug.Log ("Running outside Unity Editor. iOS-specific shader modifications activated.");
			Shader.EnableKeyword ("IOSBUILD_IPADAIR1");
			#endif
			break;

		case UnityEngine.iOS.DeviceGeneration.iPadAir2:
			#if !UNITY_EDITOR
			Debug.Log ("Running outside Unity Editor. iOS-specific shader modifications activated.");
			Shader.EnableKeyword ("IOSBUILD_IPADAIR2");
			#endif
			break;

		default:
			Shader.EnableKeyword ("IOSBUILD_OFF");
			break;
		}

		Debug.Log ("IOSBUILD_OFF:" + Shader.IsKeywordEnabled("IOSBUILD_OFF"));
		Debug.Log ("IOSBUILD_IPADAIR1:" + Shader.IsKeywordEnabled("IOSBUILD_IPADAIR1"));
		Debug.Log ("IOSBUILD_IPADAIR2:" + Shader.IsKeywordEnabled("IOSBUILD_IPADAIR2"));
	}

	private void OnPreRender() 
	{
		m_renderTarget.GetComponent<Renderer> ().material.SetTexture (
			"_ObjectTex", m_FXCamera.GetComponent<GrabModelTexture> ().GetModelTexture ());
			
		m_renderTarget.GetComponent<Renderer> ().material.SetTexture (
			"_NoiseTex", GetComponent<NoiseDistribution> ().GetNoiseTexture());

		m_renderTarget.GetComponent<Renderer> ().material.SetFloat (
			"_NoiseTexSize", GetComponent<NoiseDistribution> ().GetNoiseTexture().width);

		m_renderTarget.GetComponent<Renderer> ().material.SetFloat (
			"_ScreenRes_Width", Screen.width);

		m_renderTarget.GetComponent<Renderer> ().material.SetFloat (
			"_ScreenRes_Height", Screen.height);

		m_renderTarget.GetComponent<Renderer> ().material.SetFloat (
			"_AspectRatio", Camera.main.aspect);

		m_renderTarget.GetComponent<Renderer> ().material.SetFloat (
			"_Vuforia_Aspect", m_renderTarget.transform.localScale.x/m_renderTarget.transform.localScale.z);

		/*** TEST CODE BEGIN
		Vector2 scale = new Vector2 (1.0f, 1.0f);
#if UNITY_EDITOR
		scale.x += Input.mousePosition.x / Screen.width;
		scale.y += Input.mousePosition.y / Screen.height;
#else
		if (Input.touches.Length > 0) {
			scale.x += Input.GetTouch(0).position.x / Screen.width;
			scale.y += Input.GetTouch(0).position.y / Screen.height;
		}
#endif
		DestTarget.GetComponent<Renderer> ().material.SetVector ("_TexScale", scale);
		Debug.Log ("X:" + scale.x + ", Y:" + scale.y);
		/*** TEST CODE END ***/
	}
}
