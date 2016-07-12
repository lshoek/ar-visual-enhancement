using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Camera))]
public class ReplacementShader : MonoBehaviour 
{
	[SerializeField] Shader ReplacementShaderProgram;

	void Start () 
	{
		GetComponent<Camera>().SetReplacementShader (ReplacementShaderProgram, null);
	}
}
