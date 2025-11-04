using UnityEngine;
using UnityEngine.UI;

namespace MemorySwipeGame
{
	public class RawImageArrowController : MonoBehaviour
	{
		[SerializeField] private RawImage rawImage;
		[SerializeField] private GameObject wrapRoot;

		private ArrowDirection currentDirection = ArrowDirection.None;

		private void Awake()
		{
			if (rawImage == null)
			{
				rawImage = GetComponent<RawImage>();
			}

			if (wrapRoot == null && rawImage != null)
			{
				wrapRoot = rawImage.transform.parent != null
					? rawImage.transform.parent.gameObject
					: null;
			}
		}

		public void SetDirection(ArrowDirection direction)
		{
			currentDirection = direction;
			UpdateRotation();
		}

		private void UpdateRotation()
		{
			Transform target = null;
			if (wrapRoot != null)
			{
				target = wrapRoot.transform;
			}
			else if (rawImage != null)
			{
				target = rawImage.transform;
			}

			if (target == null) return;

			float zRotation = GetRotationForDirection(currentDirection);
			Vector3 euler = target.localEulerAngles;
			euler.z = zRotation;
			target.localEulerAngles = euler;
		}

		private static float GetRotationForDirection(ArrowDirection dir)
		{
			switch (dir)
			{
				case ArrowDirection.Down: return 0f;      // ↓
				case ArrowDirection.Up: return 180f;     // ↑
				case ArrowDirection.Left: return -90f;       // ←
				case ArrowDirection.Right: return 90f;    // →
				default: return 0f;
			}
		}

		public void SetVisible(bool visible)
		{
			if (rawImage != null)
			{
				rawImage.gameObject.SetActive(visible);
				rawImage.enabled = visible;
			}
		}

		public ArrowDirection GetDirection()
		{
			return currentDirection;
		}
	}
}
