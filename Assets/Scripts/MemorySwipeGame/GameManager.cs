using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MemorySwipeGame
{
	public class GameManager : MonoBehaviour
	{
		[SerializeField] private float revealSeconds = 3f;
		[SerializeField] private float circleSpacingPixels = 150f;
		[SerializeField] private Vector2 screenCenterOffset = Vector2.zero;
		[SerializeField] private float inputDeadZonePixels = 40f;
		[SerializeField] private int maxSequence = 50;

		private readonly List<ArrowDirection> sequence = new List<ArrowDirection>();
		private readonly List<CircleController> circles = new List<CircleController>();

		private int recallCursor;
		private bool isShowing;
		private bool isGameOver;
		private Camera mainCam;

		// swipe tracking
		private bool isDragging;
		private Vector2 dragStartScreen;

		private void Start()
		{
			mainCam = Camera.main;
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
			if (isShowing) return; // no input during showing phase

			// Touch (Input System)
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
			// Mouse (Input System)
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
			if (sequence.Count == 0 || recallCursor >= sequence.Count) return;
			ArrowDirection dir = SwipeUtils.GetDirectionFromDelta(delta, inputDeadZonePixels);
			if (dir == ArrowDirection.None) return;

			ArrowDirection expected = sequence[recallCursor];
			if (dir == expected)
			{
				recallCursor++;
				if (recallCursor >= sequence.Count)
				{
					StartCoroutine(BeginNextRound());
				}
			}
			else
			{
				GameOver();
			}
		}

		private void StartNewGame()
		{
			StopAllCoroutines();
			sequence.Clear();
			recallCursor = 0;
			isShowing = false;
			isGameOver = false;
			EnsureCircleCount(0);
			StartCoroutine(BeginNextRound());
		}

		private IEnumerator BeginNextRound()
		{
			isShowing = true;
			recallCursor = 0;

			// add a new direction
			ArrowDirection newDir = GetRandomDirection();
			sequence.Add(newDir);
			if (sequence.Count > maxSequence)
			{
				sequence.RemoveAt(0);
			}

			// ensure a circle exists for each item in the sequence
			EnsureCircleCount(sequence.Count);
			LayoutCircles();

			// memorize on the newest circle and reveal for a few seconds
			int newestIndex = sequence.Count - 1;
			CircleController newest = circles[newestIndex];
			newest.Memorize(newDir);
			newest.RevealForSeconds(revealSeconds);

			float end = Time.unscaledTime + revealSeconds;
			while (Time.unscaledTime < end)
			{
				yield return null;
			}
			newest.Hide();

			// ready for recall
			isShowing = false;
			yield break;
		}

		private void EnsureCircleCount(int count)
		{
			while (circles.Count < count)
			{
				var go = new GameObject("Circle_" + circles.Count);
				var circle = go.AddComponent<CircleController>();
				circle.sequenceIndex = circles.Count;
				circles.Add(circle);
			}
			while (circles.Count > count)
			{
				var last = circles[circles.Count - 1];
				circles.RemoveAt(circles.Count - 1);
				if (last != null) Destroy(last.gameObject);
			}
		}

		private void LayoutCircles()
		{
			int n = circles.Count;
			if (n <= 0) return;

			float totalWidth = (n - 1) * circleSpacingPixels;
			Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f) + screenCenterOffset;
			float startX = center.x - totalWidth * 0.5f;
			for (int i = 0; i < n; i++)
			{
				float x = startX + i * circleSpacingPixels;
				Vector2 pos = new Vector2(x, center.y);
				circles[i].sequenceIndex = i;
				circles[i].SetPosition(pos);
			}
		}

		private ArrowDirection GetRandomDirection()
		{
			int v = Random.Range(0, 4);
			switch (v)
			{
				case 0: return ArrowDirection.Up;
				case 1: return ArrowDirection.Down;
				case 2: return ArrowDirection.Left;
				default: return ArrowDirection.Right;
			}
		}

		private void GameOver()
		{
			isGameOver = true;
		}

		private void OnGUI()
		{
			var style = new GUIStyle(GUI.skin.label);
			style.alignment = TextAnchor.UpperCenter;
			style.fontSize = 24;
			Rect r = new Rect(0, 10, Screen.width, 40);
			if (isGameOver)
			{
				GUI.Label(r, "Game Over - Tap/Click to Retry", style);
			}
			else if (isShowing)
			{
				GUI.Label(r, "Memorize the arrow...", style);
			}
			else
			{
				GUI.Label(r, "Swipe the sequence in order", style);
			}

			var sub = new GUIStyle(style);
			sub.fontSize = 16;
			GUI.Label(new Rect(0, 44, Screen.width, 30), "Length: " + sequence.Count + "  Progress: " + recallCursor + "/" + sequence.Count, sub);
		}
	}
}


