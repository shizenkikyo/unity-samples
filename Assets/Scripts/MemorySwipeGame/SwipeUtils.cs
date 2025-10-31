using UnityEngine;

namespace MemorySwipeGame
{
	public static class SwipeUtils
	{
		public static ArrowDirection GetDirectionFromDelta(Vector2 delta, float deadZonePixels)
		{
			if (delta.magnitude < deadZonePixels)
			{
				return ArrowDirection.None;
			}

			bool isHorizontal = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y);
			if (isHorizontal)
			{
				return delta.x > 0 ? ArrowDirection.Right : ArrowDirection.Left;
			}
			else
			{
				return delta.y > 0 ? ArrowDirection.Up : ArrowDirection.Down;
			}
		}
	}
}


