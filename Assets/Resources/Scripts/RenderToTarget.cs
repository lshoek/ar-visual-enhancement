using UnityEngine;
using System.Collections;

[RequireComponent (typeof(Camera))]
public class RenderToTarget : MonoBehaviour 
{
	void Awake () {
        GetComponent<Camera>().targetTexture = new RenderTexture(Screen.width, Screen.height, 8);
	}
}
