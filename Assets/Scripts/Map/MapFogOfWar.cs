using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class MapFogOfWar : MonoBehaviour
{
	[Header("References")]
	public RectTransform fogRect;
	[FormerlySerializedAs("fogLayerA")]
	public Image fogImage;
	public List<Sprite> fogFrames = new List<Sprite>();

	[Header("Smooth Cloud Animation (Ping-Pong 1-2-3-4-3-2-1)")]
	[Tooltip("How fast it transitions between frames (lower = smoother/dreamier)")]
	public float frameRate = 0.7f;

	[Header("Position & Step Movement")]
	[Tooltip("Extra vertical offset above the highest visible node")]
	public float yOffsetAboveVisible = 35f;
	[Tooltip("How long it takes to glide up on each player step")]
	public float stepMoveDuration = 0.5f;

	[Header("Intro Roll-In Settings")]
	[Tooltip("How far below the target line the fog overshoots before bouncing back")]
	public float introOvershootDistance = 160f;
	[Tooltip("Brief pause at the lowest overshoot point before pulling back")]
	public float introOvershootPause = 0.15f;
	public float introBounceBackDuration = 0.65f;

	[Header("Clear / End of Map")]
	public float clearExtraHeight = 600f;
	public float clearFadeDuration = 1.0f;

	public bool IsAnimating => moveCoroutine != null;

	private CanvasGroup canvasGroup;
	private Coroutine moveCoroutine;
	private Material runtimeFogMat;

	private List<int> pingPongSequence = new List<int>();
	private int sequenceIndex = 0;
	private float blendTimer = 0f;
	private bool isCleared = false;

	private static readonly int SecondTexProp = Shader.PropertyToID("_SecondTex");
	private static readonly int BlendProp = Shader.PropertyToID("_Blend");

	private void Awake()
	{
		if (fogRect == null) fogRect = GetComponent<RectTransform>();
		canvasGroup = GetComponent<CanvasGroup>();

		// Enforce bottom pivot so anchoredPosition.y is always the bottom edge of the fog
		fogRect.pivot = new Vector2(0.5f, 0f);

		BuildPingPongSequence();

		if (fogFrames.Count > 0 && fogImage != null)
		{
			if (fogImage.material != null && fogImage.material.HasProperty(BlendProp))
			{
				runtimeFogMat = new Material(fogImage.material);
				fogImage.material = runtimeFogMat;
			}

			ApplyCrossfadeSprites(0, 1 % pingPongSequence.Count, 0f);
		}
	}

	private void OnDestroy()
	{
		if (runtimeFogMat != null)
			Destroy(runtimeFogMat);
	}

	private void BuildPingPongSequence()
	{
		pingPongSequence.Clear();
		int count = fogFrames.Count;
		if (count == 0) return;

		// Forward: 0, 1, 2, 3
		for (int i = 0; i < count; i++)
			pingPongSequence.Add(i);

		// Backward without repeating endpoints: 2, 1
		for (int i = count - 2; i > 0; i--)
			pingPongSequence.Add(i);
	}

	private void Update()
	{
		AnimateClouds();
	}

	private void AnimateClouds()
	{
		if (pingPongSequence.Count < 2 || fogImage == null) return;

		blendTimer += Time.deltaTime * frameRate;
		while (blendTimer >= 1f)
		{
			blendTimer -= 1f;
			sequenceIndex = (sequenceIndex + 1) % pingPongSequence.Count;
		}

		int nextSeqIndex = (sequenceIndex + 1) % pingPongSequence.Count;
		float smoothBlend = 0.5f - 0.5f * Mathf.Cos(blendTimer * Mathf.PI);

		ApplyCrossfadeSprites(sequenceIndex, nextSeqIndex, smoothBlend);
	}

	private void ApplyCrossfadeSprites(int seqIdxA, int seqIdxB, float blend)
	{
		Sprite spriteA = fogFrames[pingPongSequence[seqIdxA]];
		Sprite spriteB = fogFrames[pingPongSequence[seqIdxB]];

		if (fogImage.sprite != spriteA)
			fogImage.sprite = spriteA;

		if (runtimeFogMat != null)
		{
			runtimeFogMat.SetTexture(SecondTexProp, spriteB.texture);
			runtimeFogMat.SetFloat(BlendProp, blend);
		}
	}

	public void HideAboveMap(float startTopY)
	{
		if (moveCoroutine != null)
		{
			StopCoroutine(moveCoroutine);
			moveCoroutine = null;
		}

		isCleared = false;
		gameObject.SetActive(true);
		canvasGroup.alpha = 1f;
		SetFogY(startTopY);
	}

	public void PlayIntro(float startTopY, float targetNodeY, float rollDownDuration)
	{
		isCleared = false;
		gameObject.SetActive(true);
		canvasGroup.alpha = 1f;

		float finalTargetY = targetNodeY + yOffsetAboveVisible;

		if (moveCoroutine != null) StopCoroutine(moveCoroutine);
		moveCoroutine = StartCoroutine(IntroRoutine(startTopY, finalTargetY, rollDownDuration));
	}

	public void SetPositionImmediate(float highestVisibleNodeY, bool allNodesRevealed)
	{
		if (moveCoroutine != null)
		{
			StopCoroutine(moveCoroutine);
			moveCoroutine = null;
		}

		if (allNodesRevealed)
		{
			isCleared = true;
			canvasGroup.alpha = 0f;
			gameObject.SetActive(false);
		}
		else
		{
			isCleared = false;
			gameObject.SetActive(true);
			canvasGroup.alpha = 1f;
			SetFogY(highestVisibleNodeY + yOffsetAboveVisible);
		}
	}

	public void MoveToNodeY(float highestVisibleNodeY, bool allNodesRevealed)
	{
		if (isCleared) return;

		if (moveCoroutine != null) StopCoroutine(moveCoroutine);

		if (allNodesRevealed)
		{
			isCleared = true;
			moveCoroutine = StartCoroutine(ClearFogRoutine(highestVisibleNodeY + clearExtraHeight));
		}
		else
		{
			float targetY = highestVisibleNodeY + yOffsetAboveVisible;
			moveCoroutine = StartCoroutine(GlideToYRoutine(targetY, stepMoveDuration));
		}
	}

	private IEnumerator IntroRoutine(float startY, float finalY, float rollDownDuration)
	{
		SetFogY(startY);
		float overshootY = finalY - introOvershootDistance;

		yield return GlideToYRoutine(overshootY, rollDownDuration);

		if (introOvershootPause > 0f)
			yield return new WaitForSeconds(introOvershootPause);

		yield return GlideToYRoutine(finalY, introBounceBackDuration);

		moveCoroutine = null;
	}

	private IEnumerator GlideToYRoutine(float targetY, float duration)
	{
		float startY = fogRect.anchoredPosition.y;
		float elapsed = 0f;

		while (elapsed < duration)
		{
			elapsed += Time.deltaTime;
			float t = Mathf.Clamp01(elapsed / duration);
			float eased = Mathf.SmoothStep(0f, 1f, t);
			SetFogY(Mathf.Lerp(startY, targetY, eased));
			yield return null;
		}

		SetFogY(targetY);
	}

	private IEnumerator ClearFogRoutine(float exitY)
	{
		float startY = fogRect.anchoredPosition.y;
		float startAlpha = canvasGroup.alpha;
		float elapsed = 0f;

		while (elapsed < clearFadeDuration)
		{
			elapsed += Time.deltaTime;
			float t = Mathf.Clamp01(elapsed / clearFadeDuration);
			float eased = Mathf.SmoothStep(0f, 1f, t);

			SetFogY(Mathf.Lerp(startY, exitY, eased));
			canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, eased);
			yield return null;
		}

		SetFogY(exitY);
		canvasGroup.alpha = 0f;
		gameObject.SetActive(false);
		moveCoroutine = null;
	}

	private void SetFogY(float y)
	{
		Vector2 pos = fogRect.anchoredPosition;
		pos.y = y;
		fogRect.anchoredPosition = pos;
	}
}