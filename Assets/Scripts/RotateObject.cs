using UnityEngine;
using System.Collections;

public class RotateObject : MonoBehaviour 
{
	[SerializeField] GameObject Origin;
	[SerializeField] float Speed = 1.0f;

	void Update () {
		transform.RotateAround(Origin.transform.position, new Vector3(0, 1, 0), Speed);
	}
}
