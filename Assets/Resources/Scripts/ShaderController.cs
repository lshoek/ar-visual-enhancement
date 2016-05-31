using UnityEngine;
using System.Collections;

public class ShaderController : MonoBehaviour 
{
    [SerializeField] GameObject[] gameObjects;
	
	void Update () {
        foreach (GameObject g in gameObjects)
            g.GetComponent<Renderer>().material.SetFloat("_ElapsedTime", Time.time);
	}
}
