using UnityEngine;
using System.Collections;

public class TouchInstantiate : MonoBehaviour 
{
	[SerializeField] GameObject PrefabObject;

	private bool mouseReleased = false;
	private int numObjects = 0;

	void Update () 
	{
		if (Input.GetMouseButton(0)) 
		{
			if (mouseReleased) 
			{
				Vector2 mousePos = new Vector2(Input.mousePosition.x/Screen.width, Input.mousePosition.y/Screen.height);

				for (int i = 0; i < 100; i++) 
				{
					GameObject g = Instantiate(PrefabObject);
					g.transform.position = new Vector3((mousePos.x-0.5f) * 30.0f, 
					                                   (mousePos.y-0.5f) * 20.0f, 
					                                   (Random.value-0.5f) * 20.0f);
					++numObjects;
				}

				mouseReleased = false;
				Debug.Log("Number of objects:" + numObjects);
			}
		} 
		else
			mouseReleased = true;
	}
}
