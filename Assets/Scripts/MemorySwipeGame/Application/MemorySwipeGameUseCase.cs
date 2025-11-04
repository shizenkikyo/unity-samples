using System;
using System.Collections.Generic;
using MemorySwipeGame.Domain;

namespace MemorySwipeGame.Application
{
	public class MemorySwipeGameUseCase
	{
		private readonly MemorySequenceGame game;
		private readonly IRandomDirectionProvider randomDirectionProvider;
		private readonly int maxSequence;

		private IMemoryGamePresenter presenter;

		public MemorySwipeGameUseCase(int maxSequence, IRandomDirectionProvider randomDirectionProvider)
		{
			this.maxSequence = maxSequence;
			this.randomDirectionProvider = randomDirectionProvider ?? throw new ArgumentNullException(nameof(randomDirectionProvider));
			game = new MemorySequenceGame();
		}

		public void SetPresenter(IMemoryGamePresenter presenter)
		{
			this.presenter = presenter;
		}

		public void ResetGame()
		{
			game.Reset();
			presenter?.PresentGameReset();
		}

		public void PrepareNextRound()
		{
			EnsurePresenter();

			ArrowDirection newDirection = randomDirectionProvider.GetRandomDirection();
			game.AppendDirection(newDirection, maxSequence);

			presenter.PresentRoundPrepared(game.Sequence, newDirection);
		}

		public void BeginRecallPhase()
		{
			game.BeginRecall();
			presenter?.PresentRecallReady(game.Sequence.Count);
		}

		public SequenceEvaluation SubmitInput(ArrowDirection direction)
		{
			SequenceEvaluation evaluation = game.EvaluateInput(direction);
			switch (evaluation.State)
			{
				case SequenceEvaluationState.Correct:
					presenter?.PresentCorrectInput(evaluation.Progress, evaluation.SequenceLength);
					break;
				case SequenceEvaluationState.Completed:
					presenter?.PresentSequenceCompleted(evaluation.SequenceLength);
					break;
				case SequenceEvaluationState.Incorrect:
					presenter?.PresentIncorrectInput(evaluation.Expected, evaluation.Received, evaluation.Progress, evaluation.SequenceLength);
					presenter?.PresentGameOver(evaluation.SequenceLength);
					break;
			}

			return evaluation;
		}

		public IReadOnlyList<ArrowDirection> CurrentSequence => game.Sequence;
		public bool IsRecallActive => game.IsRecallActive;
		public bool IsGameOver => game.IsGameOver;

		private void EnsurePresenter()
		{
			if (presenter == null)
			{
				throw new InvalidOperationException("IMemoryGamePresenter has not been assigned.");
			}
		}
	}
}
