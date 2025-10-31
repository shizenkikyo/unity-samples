using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace MemorySwipeGame
{
	public class UITKGameManager : MonoBehaviour
	{
		[Header("UXML/USS")]
		[SerializeField] private UIDocument uiDocument;
		[SerializeField] private VisualTreeAsset gameUxml;
		[SerializeField] private StyleSheet gameUss;

		[Header("Gameplay")]
		[SerializeField] private float revealSeconds = 3f;
		[SerializeField] private float circleSpacingPixels = 150f;
		[SerializeField] private float circleRadiusPixels = 60f;
		[SerializeField] private int maxSequence = 50;
		[SerializeField] private Vector2 centerOffset = Vector2.zero;
		[SerializeField] private float inputDeadZonePixels = 40f;

		private Label topMessage;
		private VisualElement circlesContainer;

		private readonly List<ArrowDirection> sequence = new List<ArrowDirection>();
		private readonly List<CircleUI> circles = new List<CircleUI>();

		private int recallCursor;
		private bool isShowing;
		private bool isGameOver;

		private bool isDragging;
		private Vector2 dragStartScreen;

		private struct CircleUI
		{
			public VisualElement root;
			public VisualElement fill;
			public VisualElement outline;
			public Label arrow;
			public ArrowDirection memorized;
		}

		private void Awake()
		{
			EnsureUIDocument();
			EnsurePanelSettings();
			BuildUI();
		}

		private void Start()
		{
			StartNewGame();
		}

		private void EnsureUIDocument()
		{
			if (uiDocument == null)
			{
				uiDocument = GetComponent<UIDocument>();
				if (uiDocument == null)
				{
					uiDocument = gameObject.AddComponent<UIDocument>();
				}
			}
		}

		private void EnsurePanelSettings()
		{
			if (uiDocument != null && uiDocument.panelSettings == null)
			{
				var ps = ScriptableObject.CreateInstance<PanelSettings>();
				ps.scaleMode = PanelScaleMode.ScaleWithScreenSize;
				ps.referenceResolution = new Vector2Int(1080, 1920);
				ps.match = 0.5f;
				uiDocument.panelSettings = ps;
			}
		}

		private void BuildUI()
		{
			var root = uiDocument.rootVisualElement;
			root.Clear();
			if (gameUss != null && !root.styleSheets.Contains(gameUss))
			{
				root.styleSheets.Add(gameUss);
			}
			if (gameUxml != null)
			{
				var tree = gameUxml.Instantiate();
				root.Add(tree);
			}
			else
			{
				// Fallback: build minimal structure programmatically
				var rootVe = new VisualElement();
				rootVe.name = "Root";
				rootVe.style.width = Length.Percent(100);
				rootVe.style.height = Length.Percent(100);
				root.Add(rootVe);

				topMessage = new Label("");
				topMessage.name = "TopMessage";
				topMessage.style.unityTextAlign = TextAnchor.UpperCenter;
				topMessage.style.fontSize = 20;
				topMessage.style.marginTop = 8;
				rootVe.Add(topMessage);

				circlesContainer = new VisualElement();
				circlesContainer.name = "CirclesContainer";
				circlesContainer.style.flexGrow = 1f;
				circlesContainer.style.width = Length.Percent(100);
				circlesContainer.style.position = Position.Relative;
				rootVe.Add(circlesContainer);

				Debug.LogWarning("UITKGameManager: gameUxml not assigned. Built fallback UI at runtime.");
			}

			if (topMessage == null) topMessage = root.Q<Label>("TopMessage");
			if (circlesContainer == null) circlesContainer = root.Q<VisualElement>("CirclesContainer");

			// Ensure container uses relative positioning for absolute children
			if (circlesContainer != null && circlesContainer.resolvedStyle.position != Position.Relative)
			{
				circlesContainer.style.position = Position.Relative;
			}
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
			if (isShowing) return;

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
			UpdateTopMessage("Memorize the arrow...");
			StartCoroutine(BeginNextRound());
		}

		private IEnumerator BeginNextRound()
		{
			isShowing = true;
			recallCursor = 0;

			ArrowDirection newDir = GetRandomDirection();
			sequence.Add(newDir);
			if (sequence.Count > maxSequence)
			{
				sequence.RemoveAt(0);
			}

			EnsureCircleCount(sequence.Count);
			LayoutCircles();

			int newestIndex = sequence.Count - 1;
			var newest = circles[newestIndex];
			newest.memorized = newDir;
			Debug.Log($"Circle #{newestIndex + 1} 方向: {newDir}");
			SetArrowVisible(newest, true);
			SetArrowChar(newest, newDir);

			float end = Time.unscaledTime + revealSeconds;
			while (Time.unscaledTime < end)
			{
				yield return null;
			}
			SetArrowVisible(newest, false);

			isShowing = false;
			UpdateTopMessage("Swipe the sequence in order");
		}

		private void EnsureCircleCount(int count)
		{
			while (circles.Count < count)
			{
				circles.Add(CreateCircle(circles.Count));
			}
			while (circles.Count > count)
			{
				var last = circles[circles.Count - 1];
				circles.RemoveAt(circles.Count - 1);
				if (last.root != null) last.root.RemoveFromHierarchy();
			}
		}

		private CircleUI CreateCircle(int index)
		{
			CircleUI c = new CircleUI();
			c.root = new VisualElement();
			c.root.name = "circle_" + index;
			c.root.AddToClassList("circle-root");
			c.root.style.width = circleRadiusPixels * 2f;
			c.root.style.height = circleRadiusPixels * 2f;
			SetCornerRadius(c.root, circleRadiusPixels);

			c.fill = new VisualElement();
			c.fill.AddToClassList("circle-fill");
			c.fill.style.width = Length.Percent(100);
			c.fill.style.height = Length.Percent(100);
			SetCornerRadius(c.fill, circleRadiusPixels);
			c.root.Add(c.fill);

			c.outline = new VisualElement();
			c.outline.AddToClassList("circle-outline");
			c.outline.style.position = Position.Absolute;
			c.outline.style.left = -2;
			c.outline.style.top = -2;
			c.outline.style.width = circleRadiusPixels * 2f + 4f;
			c.outline.style.height = circleRadiusPixels * 2f + 4f;
			SetCornerRadius(c.outline, circleRadiusPixels + 2f);
			c.root.Add(c.outline);

			c.arrow = new Label("");
			c.arrow.AddToClassList("circle-arrow");
			c.arrow.style.position = Position.Absolute;
			c.arrow.style.left = 0;
			c.arrow.style.top = 0;
			c.arrow.style.width = Length.Percent(100);
			c.arrow.style.height = Length.Percent(100);
			c.arrow.style.unityTextAlign = TextAnchor.MiddleCenter;
			c.root.Add(c.arrow);

			circlesContainer.Add(c.root);
			return c;
		}

		private void LayoutCircles()
		{
			int n = circles.Count;
			if (n <= 0) return;

			float totalWidth = (n - 1) * circleSpacingPixels;
			Vector2 center = GetPanelCenter() + centerOffset;
			float startX = center.x - totalWidth * 0.5f;
			for (int i = 0; i < n; i++)
			{
				float x = startX + i * circleSpacingPixels;
				float y = center.y;
				var c = circles[i].root;
				c.style.left = x - circleRadiusPixels;
				c.style.top = y - circleRadiusPixels;
			}
		}

		private Vector2 GetPanelCenter()
		{
			var panel = uiDocument.rootVisualElement;
			var size = panel.layout.size;
			if (size == Vector2.zero)
			{
				// Estimate with Screen if layout not ready yet
				return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
			}
			return size * 0.5f;
		}

		private void SetArrowVisible(CircleUI c, bool visible)
		{
			c.arrow.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
		}

		private void SetArrowChar(CircleUI c, ArrowDirection dir)
		{
			switch (dir)
			{
				case ArrowDirection.Up: c.arrow.text = "\u2191"; break;
				case ArrowDirection.Down: c.arrow.text = "\u2193"; break;
				case ArrowDirection.Left: c.arrow.text = "\u2190"; break;
				case ArrowDirection.Right: c.arrow.text = "\u2192"; break;
				default: c.arrow.text = string.Empty; break;
			}
		}

		private void SetCornerRadius(VisualElement ve, float radius)
		{
			ve.style.borderTopLeftRadius = radius;
			ve.style.borderTopRightRadius = radius;
			ve.style.borderBottomLeftRadius = radius;
			ve.style.borderBottomRightRadius = radius;
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
			UpdateTopMessage("Game Over - Tap/Click to Retry");
		}

		private void UpdateTopMessage(string msg)
		{
			if (topMessage != null) topMessage.text = msg;
		}
	}
}


