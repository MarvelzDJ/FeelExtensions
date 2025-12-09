using System.Collections;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using UnityEngine;

/// <summary>
/// Alternative Simple feedback for animating scale with explicit start and end values
/// </summary>
[AddComponentMenu("")]
[FeedbackPath("Transform/Simple Scale")]
[FeedbackHelp("Simple scale animation from start to end value. You can enable/disable animation for each axis separately and configure easing for each axis.")]
public class MMF_SimpleScale : MMF_Feedback
{
	/// scale value modes
	public enum ScaleModes { Absolute, Relative }

	/// target value types for start scale
	public enum StartScaleValueTypes { Custom, OffsetFromInitial, Initial, Current }

	/// target value types for end scale
	public enum EndScaleValueTypes { Custom, OffsetFromInitial, Initial }

	/// time modes
	public enum TimeModes { Duration, Speed }

	/// a static bool used to disable all feedbacks of this type at once
	public static bool FeedbackTypeAuthorized = true;

	#if UNITY_EDITOR
	public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.TransformColor; } }
	public override bool EvaluateRequiresSetup() { return (TargetTransform == null); }
	public override string RequiredTargetText { get { return TargetTransform != null ? TargetTransform.name : ""; } }
	#endif

	public override bool HasAutomatedTargetAcquisition => true;
	protected override void AutomateTargetAcquisition() => TargetTransform = FindAutomatedTarget<Transform>();

	[MMFInspectorGroup("Animation Target", true, 12)]
	/// the object to animate
	[Tooltip("The object whose scale will be animated")]
	public Transform TargetTransform;

	[MMFInspectorGroup("Time Settings", true, 13)]
	/// time mode
	[Tooltip("Duration: fixed duration\nSpeed: animation speed (duration is calculated automatically as distance/speed)")]
	public TimeModes TimeMode = TimeModes.Duration;

	/// animation duration in seconds
	[Tooltip("Animation duration in seconds")]
	[MMFEnumCondition("TimeMode", (int)TimeModes.Duration)]
	public float Duration = 0.3f;

	/// animation speed
	[Tooltip("Animation speed. Duration = Vector3.Distance(startScale, endScale) / Speed")]
	[MMFEnumCondition("TimeMode", (int)TimeModes.Speed)]
	public float Speed = 5f;

	[MMFInspectorGroup("Start Value", true, 14)]
	/// start value type
	[Tooltip("Custom: custom value\nOffsetFromInitial: offset from initial scale\nInitial: initial scale of the object\nCurrent: current scale of the object")]
	public StartScaleValueTypes StartValueType = StartScaleValueTypes.Custom;

	/// start value mode
	[Tooltip("Absolute: absolute value is used\nRelative: value is multiplied by initial scale")]
	[MMFEnumCondition("StartValueType", (int)StartScaleValueTypes.Custom, (int)StartScaleValueTypes.OffsetFromInitial)]
	public ScaleModes StartMode = ScaleModes.Absolute;

	/// start scale value
	[Tooltip("Scale value for animation start")]
	[MMFEnumCondition("StartValueType", (int)StartScaleValueTypes.Custom, (int)StartScaleValueTypes.OffsetFromInitial)]
	public Vector3 StartValue = Vector3.one;

	[MMFInspectorGroup("End Value", true, 15)]
	/// end value type
	[Tooltip("Custom: custom value\nOffsetFromInitial: offset from initial scale\nInitial: initial scale of the object")]
	public EndScaleValueTypes EndValueType = EndScaleValueTypes.Custom;

	/// end value mode
	[Tooltip("Absolute: absolute value is used\nRelative: value is multiplied by initial scale")]
	[MMFEnumCondition("EndValueType", (int)EndScaleValueTypes.Custom, (int)EndScaleValueTypes.OffsetFromInitial)]
	public ScaleModes EndMode = ScaleModes.Absolute;

	/// end scale value
	[Tooltip("Scale value for animation end")]
	[MMFEnumCondition("EndValueType", (int)EndScaleValueTypes.Custom, (int)EndScaleValueTypes.OffsetFromInitial)]
	public Vector3 EndValue = Vector3.one * 1.5f;

	[MMFInspectorGroup("Axis Settings", true, 16)]
	/// whether to animate X axis
	[Tooltip("Enable animation on X axis")]
	public bool AnimateX = true;

	/// tween type for X axis
	[Tooltip("Tween type for X axis")]
	[MMFCondition("AnimateX", true)]
	public MMTweenType TweenTypeX = new MMTweenType(MMTween.MMTweenCurve.EaseInOutCubic);

	/// whether to animate Y axis
	[Tooltip("Enable animation on Y axis")]
	public bool AnimateY = true;

	/// tween type for Y axis
	[Tooltip("Tween type for Y axis")]
	[MMFCondition("AnimateY", true)]
	public MMTweenType TweenTypeY = new MMTweenType(MMTween.MMTweenCurve.EaseInOutCubic);

	/// whether to animate Z axis
	[Tooltip("Enable animation on Z axis")]
	public bool AnimateZ = true;

	/// tween type for Z axis
	[Tooltip("Tween type for Z axis")]
	[MMFCondition("AnimateZ", true)]
	public MMTweenType TweenTypeZ = new MMTweenType(MMTween.MMTweenCurve.EaseInOutCubic);

	public override float FeedbackDuration
	{
		get { return ApplyTimeMultiplier(TimeMode == TimeModes.Duration ? Duration : _calculatedDuration); }
		set { Duration = value; }
	}

	protected Coroutine _coroutine;
	protected Vector3 _initialScale;
	protected float _calculatedDuration;

	/// <summary>
	/// On initialization, stores the initial scale
	/// </summary>
	protected override void CustomInitialization(MMF_Player owner)
	{
		base.CustomInitialization(owner);
		if (Active && (TargetTransform != null))
		{
			_initialScale = TargetTransform.localScale;
		}
	}

	/// <summary>
	/// Triggers the scale animation
	/// </summary>
	protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
	{
		if (!Active || !FeedbackTypeAuthorized || (TargetTransform == null))
		{
			return;
		}

		if (_coroutine != null)
		{
			Owner.StopCoroutine(_coroutine);
		}

		_coroutine = Owner.StartCoroutine(AnimateScaleCoroutine());
	}

	/// <summary>
	/// Coroutine for animating scale
	/// </summary>
	protected virtual IEnumerator AnimateScaleCoroutine()
	{
		if (TargetTransform == null)
		{
			yield break;
		}

		Vector3 fromScale = GetStartScale();
		Vector3 toScale = GetEndScale();

		// Calculate duration based on mode
		float duration;
		if (TimeMode == TimeModes.Speed)
		{
			float distance = Vector3.Distance(fromScale, toScale);
			duration = Speed > 0 ? distance / Speed : 0f;
			_calculatedDuration = duration;
		}
		else
		{
			duration = Duration;
		}

		if (duration == 0f)
		{
			TargetTransform.localScale = NormalPlayDirection ? toScale : fromScale;
			yield break;
		}

		float journey = NormalPlayDirection ? 0f : duration;
		IsPlaying = true;

		Vector3 animFromScale = NormalPlayDirection ? fromScale : toScale;
		Vector3 animToScale = NormalPlayDirection ? toScale : fromScale;

		while ((journey >= 0) && (journey <= duration))
		{
			float percent = Mathf.Clamp01(journey / duration);
			Vector3 newScale = TargetTransform.localScale;

			// Animate X
			if (AnimateX)
			{
				float t = TweenTypeX.Evaluate(percent);
				newScale.x = Mathf.LerpUnclamped(animFromScale.x, animToScale.x, t);
			}

			// Animate Y
			if (AnimateY)
			{
				float t = TweenTypeY.Evaluate(percent);
				newScale.y = Mathf.LerpUnclamped(animFromScale.y, animToScale.y, t);
			}

			// Animate Z
			if (AnimateZ)
			{
				float t = TweenTypeZ.Evaluate(percent);
				newScale.z = Mathf.LerpUnclamped(animFromScale.z, animToScale.z, t);
			}

			TargetTransform.localScale = newScale;

			journey += NormalPlayDirection ? FeedbackDeltaTime : -FeedbackDeltaTime;
			yield return null;
		}

		// Set final value
		Vector3 finalScale = TargetTransform.localScale;

		if (AnimateX) finalScale.x = animToScale.x;
		if (AnimateY) finalScale.y = animToScale.y;
		if (AnimateZ) finalScale.z = animToScale.z;

		TargetTransform.localScale = finalScale;

		IsPlaying = false;
		_coroutine = null;
	}

	/// <summary>
	/// Gets the start scale value
	/// </summary>
	protected virtual Vector3 GetStartScale()
	{
		Vector3 result = Vector3.one;

		switch (StartValueType)
		{
			case StartScaleValueTypes.Custom:
				result = StartValue;
				if (StartMode == ScaleModes.Relative)
				{
					result = Vector3.Scale(_initialScale, result);
				}
				break;

			case StartScaleValueTypes.OffsetFromInitial:
				if (StartMode == ScaleModes.Absolute)
				{
					result = _initialScale + StartValue;
				}
				else
				{
					result = _initialScale + Vector3.Scale(_initialScale, StartValue);
				}
				break;

			case StartScaleValueTypes.Initial:
				result = _initialScale;
				break;

			case StartScaleValueTypes.Current:
				result = TargetTransform.localScale;
				break;
		}

		return result;
	}

	/// <summary>
	/// Gets the end scale value
	/// </summary>
	protected virtual Vector3 GetEndScale()
	{
		Vector3 result = Vector3.one;

		switch (EndValueType)
		{
			case EndScaleValueTypes.Custom:
				result = EndValue;
				if (EndMode == ScaleModes.Relative)
				{
					result = Vector3.Scale(_initialScale, result);
				}
				break;

			case EndScaleValueTypes.OffsetFromInitial:
				if (EndMode == ScaleModes.Absolute)
				{
					result = _initialScale + EndValue;
				}
				else
				{
					result = _initialScale + Vector3.Scale(_initialScale, EndValue);
				}
				break;

			case EndScaleValueTypes.Initial:
				result = _initialScale;
				break;
		}

		return result;
	}

	/// <summary>
	/// Stops the animation
	/// </summary>
	protected override void CustomStopFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
	{
		if (!Active || !FeedbackTypeAuthorized || (_coroutine == null))
		{
			return;
		}

		IsPlaying = false;
		Owner.StopCoroutine(_coroutine);
		_coroutine = null;
	}

	/// <summary>
	/// On disable, resets the coroutine
	/// </summary>
	public override void OnDisable()
	{
		_coroutine = null;
	}

	/// <summary>
	/// Restores initial values
	/// </summary>
	protected override void CustomRestoreInitialValues()
	{
		if (!Active || !FeedbackTypeAuthorized || (TargetTransform == null))
		{
			return;
		}

		TargetTransform.localScale = _initialScale;
	}

	#if UNITY_EDITOR
	public override string RequiresSetupText
	{
		get
		{
			if (TargetTransform == null)
			{
				return "Target Transform needs to be set for this feedback to work properly.";
			}

			return "";
		}
	}
	#endif
}
