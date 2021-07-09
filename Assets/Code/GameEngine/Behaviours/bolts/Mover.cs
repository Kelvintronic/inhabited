using UnityEngine;
using System.Collections;

namespace GameEngine
{
	public class Mover : MonoBehaviour
	{
		public float speed;
		public float distance;

		private Vector3 _startPosition;

		void Start()
		{
			GetComponent<Rigidbody2D>().velocity = transform.up * speed;
			_startPosition = transform.position;
		}

		void FixedUpdate()
		{
			if (Vector3.Distance(_startPosition, transform.position) >= distance)
			{
				Destroy(gameObject);
			}
		}
	}
}