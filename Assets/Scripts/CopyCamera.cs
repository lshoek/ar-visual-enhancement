using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Camera))]
public class CopyCamera : MonoBehaviour 
{
	[SerializeField] Camera SrcCamera;

	private float _initialdepth;

	void Start()
	{
		_initialdepth = GetComponent<Camera>().depth;
	}

	void LateUpdate () 
	{
		this.transform.parent.position= SrcCamera.transform.position;
		this.transform.parent.rotation = SrcCamera.transform.rotation;
		GetComponent<Camera>().projectionMatrix = SrcCamera.projectionMatrix;
		
		GetComponent<Camera>().depth = _initialdepth;
	}
}
