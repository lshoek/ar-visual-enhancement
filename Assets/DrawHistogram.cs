using UnityEngine;
using System.Collections;

[RequireComponent(typeof(ParticleSystem))]
public class DrawHistogram : MonoBehaviour 
{
	private const float GRAPH_SIZE_X = 100.0f;
	private const float GRAPH_SIZE_Y = 10.0f;
	private const int COLOR_DEPTH = 256;

	private ParticleSystem.Particle[] points_r;
	private ParticleSystem.Particle[] points_g;
	private ParticleSystem.Particle[] points_b;

	private int[,] m_histogram;
	private int m_max;
	private bool m_newData;

	private void Start () 
	{	
		m_max = 1;
		m_newData = false;
	}

	private void CreatePoints()
	{
		int SIZE = m_histogram.GetUpperBound (1)+1;

		points_r = new ParticleSystem.Particle[SIZE];
		points_g = new ParticleSystem.Particle[SIZE];
		points_b = new ParticleSystem.Particle[SIZE];

		float incr = GRAPH_SIZE_X / (SIZE - 1);
		for (int i = 0; i < SIZE; i++) 
		{
			float x = i * incr;
			points_r[i].position = new Vector3 (x, ((float)m_histogram [0, i] / m_max)*GRAPH_SIZE_Y, 0.0f);
			points_g[i].position = new Vector3 (x, ((float)m_histogram [1, i] / m_max)*GRAPH_SIZE_Y, 10.0f);
			points_b[i].position = new Vector3 (x, ((float)m_histogram [2, i] / m_max)*GRAPH_SIZE_Y, 20.0f);

			points_r[i].color = new Color(x, 0f, 0f); points_r[i].size = 0.2f;
			points_g[i].color = new Color(0f, x, 0f); points_g[i].size = 0.2f;
			points_b[i].color = new Color(0f, 0f, x); points_b[i].size = 0.2f;
		}
	}

	private void Update()
	{
		if (m_newData) {
			CreatePoints ();
			m_newData = false;
		}
		//GetComponent<ParticleSystem> ().SetParticles (points_r, COLOR_DEPTH);
		//GetComponent<ParticleSystem> ().SetParticles (points_g, COLOR_DEPTH);
		GetComponent<ParticleSystem> ().SetParticles (points_b, COLOR_DEPTH);
	}

	public void PassHistogramData(int[,] data) 
	{
		m_newData = true;
		m_histogram = data;
		m_max = FindMax(data);
		Debug.Log ("Maximum: " + m_max);
	}

	private int FindMax(int[,] data)
	{
		int max = data[0, 0];
		for (int i = 0; i <= data.GetUpperBound(0); i++) 
			for (int j = 0; j <= data.GetUpperBound(1); j++) 
				if (max < data[i, j])
					max = data[i, j];
		return (max > 0) ? max : 1;
	}
}
