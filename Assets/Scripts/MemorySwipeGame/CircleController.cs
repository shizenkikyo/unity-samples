using System.Collections;
using UnityEngine;

namespace MemorySwipeGame
{
	public class CircleController : MonoBehaviour
	{
		[SerializeField] private float radiusPixels = 60f;
		[SerializeField] private Color circleColor = new Color(1f, 1f, 1f, 0.1f);
		[SerializeField] private Color outlineColor = new Color(1f, 1f, 1f, 0.9f);
		[SerializeField] private Color arrowColor = Color.white;

		public int sequenceIndex;
		public Vector2 screenCenter;
		public ArrowDirection memorizedArrow = ArrowDirection.None;

		private bool isRevealed;
		private float revealUntilTime;

		private void Awake()
		{
			// Legacy IMGUI visuals removed. This component now only stores state.
		}

		private void Update()
		{
			if (isRevealed && Time.unscaledTime >= revealUntilTime)
			{
				isRevealed = false;
			}
		}

		public void SetPosition(Vector2 centerScreen)
		{
			screenCenter = centerScreen;
		}

		public void Memorize(ArrowDirection direction)
		{
			memorizedArrow = direction;
		}

		public void RevealForSeconds(float seconds)
		{
			isRevealed = true;
			revealUntilTime = Time.unscaledTime + seconds;
		}

		public void Hide()
		{
			isRevealed = false;
		}

		public bool IsRevealed()
		{
			return isRevealed;
		}
	}
}


