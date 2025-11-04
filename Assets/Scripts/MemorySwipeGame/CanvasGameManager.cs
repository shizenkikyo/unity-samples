using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MemorySwipeGame.Application;
using MemorySwipeGame.Domain;
using MemorySwipeGame.Infrastructure;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MemorySwipeGame
{
	public class CanvasGameManager : MonoBehaviour, IMemoryGamePresenter
	{
		[Header("Prefab")]
		[SerializeField] private GameObject cursorPrefab;

		[Header("Layout")]
		[SerializeField] private float circleSpacing = 150f;
		[SerializeField] private Vector2 centerOffset = Vector2.zero;
		[SerializeField] private int maxSequence = 50;
		[SerializeField] private float historyRowYOffset = -220f;
		[SerializeField, Range(0.1f, 1f)] private float historyScale = 0.5f;

		[Header("Gameplay")]
		[SerializeField] private float revealSeconds = 3f;
		[SerializeField] private float inputDeadZonePixels = 40f;
		[SerializeField] private float nextRoundDelaySeconds = 0.75f;

		private readonly List<ArrowDirection> visibleSequence = new List<ArrowDirection>();
		private readonly List<GameObject> cursorInstances = new List<GameObject>();

		private MemorySwipeGameUseCase useCase;
		private IRandomDirectionProvider randomDirectionProvider;

		private int recallCursor;
		private bool isInputLocked;
		private bool isGameOver;
		private bool isDragging;
		private Vector2 dragStartScreen;

		private CancellationTokenSource revealCts;
		private CancellationTokenSource nextRoundCts;

		private void Awake()
		{
			randomDirectionProvider = new UnityRandomDirectionProvider();
			useCase = new MemorySwipeGameUseCase(maxSequence, randomDirectionProvider);
			useCase.SetPresenter(this);
		}

		private void Start()
		{
			StartNewGame();
		}

		private void Update()
		{
			if (isGameOver)
			{
				bool pressedToRestart =
					(Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
					|| (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
					|| (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame);
				if (pressedToRestart)
				{
					StartNewGame();
				}
				return;
			}

			HandleSwipeInput();
		}

		private void HandleSwipeInput()
		{
			if (isInputLocked) return;

			if (Touchscreen.current != null)
			{
				var touch = Touchscreen.current.primaryTouch;
				if (touch.press.wasPressedThisFrame)
				{
					isDragging = true;
					dragStartScreen = touch.position.ReadValue();
				}
				else if (touch.press.wasReleasedThisFrame)
				{
					if (isDragging)
					{
						Vector2 endPos = touch.position.ReadValue();
						Vector2 delta = endPos - dragStartScreen;
						OnSwipe(delta);
					}
					isDragging = false;
				}
			}
			else if (Mouse.current != null)
			{
				if (Mouse.current.leftButton.wasPressedThisFrame)
				{
					isDragging = true;
					dragStartScreen = Mouse.current.position.ReadValue();
				}
				else if (Mouse.current.leftButton.wasReleasedThisFrame)
				{
					if (isDragging)
					{
						Vector2 endPos = Mouse.current.position.ReadValue();
						Vector2 delta = endPos - dragStartScreen;
						OnSwipe(delta);
					}
					isDragging = false;
				}
			}
		}

		private void OnSwipe(Vector2 delta)
		{
			if (isInputLocked || isGameOver) return;

			ArrowDirection dir = SwipeUtils.GetDirectionFromDelta(delta, inputDeadZonePixels);
			if (dir == ArrowDirection.None) return;

			SequenceEvaluation evaluation = useCase.SubmitInput(dir);
			if (evaluation.State == SequenceEvaluationState.Ignored)
			{
				Debug.Log($"入力が無視されました: {dir}");
			}
			else
			{
				Debug.Log($"スワイプ方向: {dir}");
			}
		}

		private void StartNewGame()
		{
			CancelRevealTask();
			CancelNextRoundTask();

			visibleSequence.Clear();
			recallCursor = 0;
			isGameOver = false;
			isInputLocked = false;
			isDragging = false;

			EnsureCursorCount(0);
			useCase.ResetGame();
			PrepareNextRound();
		}

		private void PrepareNextRound()
		{
			if (isGameOver) return;

			CancelRevealTask();
			CancelNextRoundTask();

			isInputLocked = true;
			useCase.PrepareNextRound();
		}

		private void EnsureCursorCount(int count)
		{
			while (cursorInstances.Count < count)
			{
				if (cursorPrefab == null)
				{
					Debug.LogError("CanvasGameManager: cursorPrefab is not assigned!");
					break;
				}

				Canvas canvas = FindObjectOfType<Canvas>();
				GameObject cursor;
				if (canvas != null)
				{
					cursor = Instantiate(cursorPrefab, canvas.transform, false);
				}
				else
				{
					cursor = Instantiate(cursorPrefab);
				}
				cursor.name = "Cursor_" + cursorInstances.Count;
				cursorInstances.Add(cursor);
			}

			while (cursorInstances.Count > count)
			{
				var last = cursorInstances[cursorInstances.Count - 1];
				cursorInstances.RemoveAt(cursorInstances.Count - 1);
				if (last != null) Destroy(last);
			}
		}

		private void LayoutCursors()
		{
			int n = cursorInstances.Count;
			if (n <= 0) return;

			Canvas canvas = FindObjectOfType<Canvas>();
			if (canvas == null)
			{
				Debug.LogError("CanvasGameManager: Canvas not found!");
				return;
			}

			for (int i = 0; i < n; i++)
			{
				GameObject cursor = cursorInstances[i];
				RectTransform rt = cursor.GetComponent<RectTransform>();
				if (rt == null)
				{
					Debug.LogWarning($"CanvasGameManager: Cursor {i} has no RectTransform!");
					continue;
				}

				rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
				rt.pivot = new Vector2(0.5f, 0.5f);

				bool isNewest = (i == n - 1);
				if (isNewest)
				{
					rt.anchoredPosition = centerOffset;
					rt.localScale = Vector3.one;
				}
				else
				{
					int historyCount = n - 1;
					int historyIndex = i;
					float offsetFromCenter;
					if (historyCount == 1)
					{
						offsetFromCenter = 0f;
					}
					else if (historyCount % 2 == 1)
					{
						int centerIndex = historyCount / 2;
						offsetFromCenter = (historyIndex - centerIndex) * circleSpacing;
					}
					else
					{
						offsetFromCenter = (historyIndex - (historyCount - 1) * 0.5f) * circleSpacing;
					}

					rt.anchoredPosition = new Vector2(centerOffset.x + offsetFromCenter, centerOffset.y + historyRowYOffset);
					rt.localScale = Vector3.one * Mathf.Clamp(historyScale, 0.1f, 1f);
				}

				if (!isNewest)
				{
					RawImageArrowController arrowCtrl = cursor.GetComponent<RawImageArrowController>();
					if (arrowCtrl != null)
					{
						arrowCtrl.SetVisible(false);
					}
				}
			}
		}

		private void ApplySequenceToCursors()
		{
			for (int i = 0; i < visibleSequence.Count && i < cursorInstances.Count; i++)
			{
				RawImageArrowController controller = GetArrowController(i);
				if (controller == null) continue;

				controller.SetDirection(visibleSequence[i]);
				controller.SetVisible(false);
			}
		}

		private void StartRevealRoutine(int index)
		{
			if (index < 0 || index >= cursorInstances.Count) return;

			CancelRevealTask();

			var cts = new CancellationTokenSource();
			revealCts = cts;
			_ = RevealNewestCursorAsync(index, cts);
		}

		private async Task RevealNewestCursorAsync(int index, CancellationTokenSource cts)
		{
			var token = cts.Token;
			isInputLocked = true;

			RawImageArrowController controller = GetArrowController(index);
			if (controller != null)
			{
				controller.SetVisible(true);
				Debug.Log($"Circle #{index + 1} 方向: {visibleSequence[index]}");
			}

			try
			{
				var delaySeconds = Mathf.Max(0f, revealSeconds);
				if (delaySeconds > 0f)
				{
					await Task.Delay(TimeSpan.FromSeconds(delaySeconds), token);
				}
			}
			catch (TaskCanceledException)
			{
				if (controller != null)
				{
					controller.SetVisible(false);
				}
				return;
			}

			if (controller != null)
			{
				controller.SetVisible(false);
			}

			ClearRevealTaskReference(cts);
			useCase.BeginRecallPhase();
		}

		private void ShowCursorAsRecalled(int index)
		{
			RawImageArrowController controller = GetArrowController(index);
			if (controller != null)
			{
				controller.SetVisible(true);
			}
		}

		private RawImageArrowController GetArrowController(int index)
		{
			if (index < 0 || index >= cursorInstances.Count) return null;

			GameObject cursor = cursorInstances[index];
			if (cursor == null) return null;

			return cursor.GetComponent<RawImageArrowController>();
		}

		public void PresentGameReset()
		{
			visibleSequence.Clear();
			recallCursor = 0;
			isGameOver = false;
		}

		public void PresentRoundPrepared(IReadOnlyList<ArrowDirection> sequence, ArrowDirection newestDirection)
		{
			visibleSequence.Clear();
			visibleSequence.AddRange(sequence);
			recallCursor = 0;

			EnsureCursorCount(visibleSequence.Count);
			LayoutCursors();
			ApplySequenceToCursors();

			int newestIndex = visibleSequence.Count - 1;
			if (newestIndex >= 0)
			{
				StartRevealRoutine(newestIndex);
			}
		}

		public void PresentRecallReady(int sequenceLength)
		{
			recallCursor = 0;
			isInputLocked = false;
		}

		public void PresentCorrectInput(int progress, int sequenceLength)
		{
			recallCursor = progress;
			ShowCursorAsRecalled(progress - 1);
		}

		public void PresentSequenceCompleted(int sequenceLength)
		{
			recallCursor = sequenceLength;
			isInputLocked = true;

			if (sequenceLength > 0)
			{
				ShowCursorAsRecalled(sequenceLength - 1);
			}

			ScheduleNextRound();
		}

		public void PresentIncorrectInput(ArrowDirection expected, ArrowDirection received, int progress, int sequenceLength)
		{
			Debug.LogWarning($"不正解！期待値: {expected}, 入力: {received}");
		}

		public void PresentGameOver(int sequenceLength)
		{
			isGameOver = true;
			isInputLocked = true;
			CancelNextRoundTask();
		}

		private void OnGUI()
		{
			var style = new GUIStyle(GUI.skin.label)
			{
				alignment = TextAnchor.UpperCenter,
				fontSize = 24
			};

			Rect r = new Rect(0, 10, Screen.width, 40);
			if (isGameOver)
			{
				GUI.Label(r, "Game Over - Tap/Click to Retry", style);
			}
			else if (isInputLocked && revealCts != null)
			{
				GUI.Label(r, "Memorize the arrow...", style);
			}
			else
			{
				GUI.Label(r, "Swipe the sequence in order", style);
			}

			var sub = new GUIStyle(style)
			{
				fontSize = 16
			};

			GUI.Label(new Rect(0, 44, Screen.width, 30),
				$"Length: {visibleSequence.Count}  Progress: {recallCursor}/{visibleSequence.Count}",
					sub);
		}

		private void CancelRevealTask()
		{
			if (revealCts != null)
			{
				var cts = revealCts;
				revealCts = null;
				cts.Cancel();
				cts.Dispose();
			}
		}

		private void CancelNextRoundTask()
		{
			if (nextRoundCts != null)
			{
				var cts = nextRoundCts;
				nextRoundCts = null;
				cts.Cancel();
				cts.Dispose();
			}
		}

		private void ClearRevealTaskReference(CancellationTokenSource cts)
		{
			if (ReferenceEquals(revealCts, cts))
			{
				revealCts = null;
			}
			cts.Dispose();
		}

		private void ScheduleNextRound()
		{
			CancelNextRoundTask();

			var cts = new CancellationTokenSource();
			nextRoundCts = cts;
			_ = PrepareNextRoundAfterDelayAsync(cts);
		}

		private async Task PrepareNextRoundAfterDelayAsync(CancellationTokenSource cts)
		{
			var token = cts.Token;
			try
			{
				var delaySeconds = Mathf.Max(0f, nextRoundDelaySeconds);
				if (delaySeconds > 0f)
				{
					await Task.Delay(TimeSpan.FromSeconds(delaySeconds), token);
				}
				if (token.IsCancellationRequested)
				{
					return;
				}
			}
			catch (TaskCanceledException)
			{
				return;
			}
			finally
			{
				if (ReferenceEquals(nextRoundCts, cts))
				{
					nextRoundCts = null;
				}
				cts.Dispose();
			}

			if (!isGameOver)
			{
				PrepareNextRound();
			}
		}
	}
}
