using UnityEngine;
using MemorySwipeGame.Application;

namespace MemorySwipeGame.Infrastructure
{
	public class UnityRandomDirectionProvider : IRandomDirectionProvider
	{
		public ArrowDirection GetRandomDirection()
		{
			int value = Random.Range(0, 4);
			switch (value)
			{
				case 0: return ArrowDirection.Up;
				case 1: return ArrowDirection.Down;
				case 2: return ArrowDirection.Left;
				default: return ArrowDirection.Right;
			}
		}
	}
}
