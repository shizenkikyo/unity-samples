using System.Collections.Generic;

namespace MemorySwipeGame.Application
{
	public interface IMemoryGamePresenter
	{
		void PresentGameReset();
		void PresentRoundPrepared(IReadOnlyList<ArrowDirection> sequence, ArrowDirection newestDirection);
		void PresentRecallReady(int sequenceLength);
		void PresentCorrectInput(int progress, int sequenceLength);
		void PresentSequenceCompleted(int sequenceLength);
		void PresentIncorrectInput(ArrowDirection expected, ArrowDirection received, int progress, int sequenceLength);
		void PresentGameOver(int sequenceLength);
	}
}
