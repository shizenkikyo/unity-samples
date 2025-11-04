namespace MemorySwipeGame.Domain
{
	public enum SequenceEvaluationState
	{
		Correct,
		Completed,
		Incorrect,
		Ignored
	}

	public readonly struct SequenceEvaluation
	{
		public SequenceEvaluationState State { get; }
		public int SequenceLength { get; }
		public int Progress { get; }
		public ArrowDirection Expected { get; }
		public ArrowDirection Received { get; }

		private SequenceEvaluation(
			SequenceEvaluationState state,
			int sequenceLength,
			int progress,
			ArrowDirection expected,
			ArrowDirection received)
		{
			State = state;
			SequenceLength = sequenceLength;
			Progress = progress;
			Expected = expected;
			Received = received;
		}

		public static SequenceEvaluation Correct(int sequenceLength, int progress, ArrowDirection expected, ArrowDirection received)
			=> new SequenceEvaluation(SequenceEvaluationState.Correct, sequenceLength, progress, expected, received);

		public static SequenceEvaluation Completed(int sequenceLength, ArrowDirection expected, ArrowDirection received)
			=> new SequenceEvaluation(SequenceEvaluationState.Completed, sequenceLength, sequenceLength, expected, received);

		public static SequenceEvaluation Incorrect(int sequenceLength, int progress, ArrowDirection expected, ArrowDirection received)
			=> new SequenceEvaluation(SequenceEvaluationState.Incorrect, sequenceLength, progress, expected, received);

		public static SequenceEvaluation Ignored(int sequenceLength, int progress, ArrowDirection expected, ArrowDirection received)
			=> new SequenceEvaluation(SequenceEvaluationState.Ignored, sequenceLength, progress, expected, received);
	}
}
