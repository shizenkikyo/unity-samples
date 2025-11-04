using System.Collections.Generic;

namespace MemorySwipeGame.Domain
{
	public class MemorySequenceGame
	{
		private readonly List<ArrowDirection> sequence = new List<ArrowDirection>();

		private int recallIndex;
		private bool isRecallActive;
		private bool isGameOver;

		public IReadOnlyList<ArrowDirection> Sequence => sequence;
		public int RecallProgress => recallIndex;
		public bool IsRecallActive => isRecallActive;
		public bool IsGameOver => isGameOver;

		public void Reset()
		{
			sequence.Clear();
			recallIndex = 0;
			isRecallActive = false;
			isGameOver = false;
		}

		public void AppendDirection(ArrowDirection direction, int maxSequence)
		{
			isGameOver = false;
			isRecallActive = false;
			recallIndex = 0;

			sequence.Add(direction);
			if (maxSequence > 0 && sequence.Count > maxSequence)
			{
				sequence.RemoveAt(0);
			}
		}

		public void BeginRecall()
		{
			recallIndex = 0;
			isRecallActive = true;
		}

		public ArrowDirection PeekExpectedDirection()
		{
			if (!isRecallActive || recallIndex < 0 || recallIndex >= sequence.Count)
			{
				return ArrowDirection.None;
			}

			return sequence[recallIndex];
		}

		public SequenceEvaluation EvaluateInput(ArrowDirection input)
		{
			if (!isRecallActive || isGameOver || sequence.Count == 0)
			{
				return SequenceEvaluation.Ignored(sequence.Count, recallIndex, PeekExpectedDirection(), input);
			}

			ArrowDirection expected = PeekExpectedDirection();
			if (input == expected)
			{
				recallIndex++;
				if (recallIndex >= sequence.Count)
				{
					isRecallActive = false;
					return SequenceEvaluation.Completed(sequence.Count, expected, input);
				}

				return SequenceEvaluation.Correct(sequence.Count, recallIndex, expected, input);
			}

			isGameOver = true;
			isRecallActive = false;
			return SequenceEvaluation.Incorrect(sequence.Count, recallIndex, expected, input);
		}
	}
}
