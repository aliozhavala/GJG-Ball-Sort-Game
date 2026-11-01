using System;
using BallSort.Game.Views;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BallSort.Game.UI
{
    /// <summary>
    /// The Win panel's entrance (docs/animation-spec.md, 5.2): the screen dims and the win picture pops
    /// in; then the title pops in letter by letter, the reward pops in, and Continue pops in last and only
    /// then takes taps. A tap skips to the next step.
    /// </summary>
    /// <remarks>
    /// Real time (the board's clock and hit-stop do not apply), played at <see cref="AnimationConfig.Speed"/>.
    /// <see cref="StepStarted"/> marks the steps the feedback plays sounds on; a step a skip passes over
    /// raises nothing, the step it lands on raises at once.
    /// </remarks>
    public sealed class WinPanelAnimation : MonoBehaviour
    {
        [SerializeField] private AnimationConfig _animation;
        [Tooltip("The popup's full-screen dim (its colour's alpha is the dim at rest).")]
        [SerializeField] private Image _dim;
        [SerializeField] private RectTransform _picture;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private RectTransform _reward;
        [SerializeField] private Button _continue;

        private const float PictureOvershootAt = 0.6f;
        private const float PopFrom = 0.35f;

        private Sequence _sequence;
        private Graphic[] _continueGraphics;
        private float _dimAlpha;
        private bool _hasReward;
        private bool _skipping;
        private float _cardAt;
        private float _rewardAt;
        private float _titleDuration;
        private TweenCallback<float> _dimStep;
        private TweenCallback<float> _pictureStep;
        private TweenCallback<float> _letterStep;
        private TweenCallback<float> _rewardStep;
        private TweenCallback<float> _continueStep;
        private TweenCallback _finish;

        /// <summary>A step of the entrance started (in time, or the one a skip landed on).</summary>
        public event Action<WinStep> StepStarted;

        /// <summary>True until Continue can be pressed.</summary>
        public bool IsPlaying => _sequence != null && _sequence.IsActive();

        /// <summary>Plays the entrance from the start; <paramref name="hasReward"/> false keeps the reward step silent.</summary>
        public void Play(bool hasReward)
        {
            Kill();
            _hasReward = hasReward;
            SetDim(0f);
            _picture.localScale = Vector3.zero;
            _picture.localRotation = Quaternion.identity;
            _title.alpha = 0f;
            _reward.localScale = Vector3.zero;
            _continue.transform.localScale = Vector3.zero;
            SetContinueLive(false);

            float pop = _animation.WinPopDuration;
            _cardAt = _animation.WinCardDelay;
            _rewardAt = _cardAt + _animation.WinRewardDelay;
            float continueAt = _cardAt + _animation.WinContinueDelay;
            int letters = CountLetters();
            _titleDuration = _animation.WinTitleStart + Mathf.Max(letters - 1, 0) * _animation.WinLetterStagger +
                _animation.WinLetterDuration;
            float stampAt = _cardAt + _animation.WinStampBase + _animation.WinLetterStagger * letters;

            Sequence sequence = DOTween.Sequence();
            sequence.InsertCallback(0f, () => Raise(WinStep.Overlay));
            sequence.Insert(0f, DOVirtual.Float(0f, 1f, _animation.WinDimDuration, _dimStep).SetEase(Ease.OutQuad));
            sequence.Insert(0f, DOVirtual.Float(0f, 1f, _animation.WinPicturePopDuration, _pictureStep).SetEase(Ease.Linear));
            sequence.InsertCallback(_cardAt, () => Raise(WinStep.Card));
            sequence.Insert(_cardAt, DOVirtual.Float(0f, _titleDuration, _titleDuration, _letterStep).SetEase(Ease.Linear));
            sequence.InsertCallback(stampAt, () => Raise(WinStep.TitleStamp));
            sequence.InsertCallback(_rewardAt, () => Raise(WinStep.Reward));
            sequence.Insert(_rewardAt, DOVirtual.Float(0f, 1f, pop, _rewardStep).SetEase(Ease.OutBack));
            sequence.Insert(continueAt, DOVirtual.Float(0f, 1f, pop, _continueStep).SetEase(Ease.OutBack));
            sequence.OnComplete(_finish);
            sequence.timeScale = _animation.Speed;
            _sequence = sequence;
        }

        /// <summary>A tap on the panel: jumps to the next step (the title, the reward, then the end).</summary>
        public void Skip()
        {
            if (!IsPlaying)
            {
                return;
            }

            float now = _sequence.Elapsed(false);
            if (now < _cardAt)
            {
                JumpTo(_cardAt, WinStep.Card);
            }
            else if (now < _rewardAt && _hasReward)
            {
                JumpTo(_rewardAt, WinStep.Reward);
            }
            else
            {
                Kill();
                Finish();
            }
        }

        // Goes to a step's start without the steps passed over; the step's own sound plays now.
        private void JumpTo(float time, WinStep step)
        {
            _skipping = true;
            _sequence.Goto(time + 0.001f, true);
            _skipping = false;
            StepStarted?.Invoke(step);
        }

        private void Raise(WinStep step)
        {
            if (_skipping || (step == WinStep.Reward && !_hasReward))
            {
                return;
            }

            StepStarted?.Invoke(step);
        }

        // Everything at rest, Continue live: the entrance is over or was skipped to its end.
        private void Finish()
        {
            _sequence = null;
            SetDim(1f);
            _picture.localScale = Vector3.one;
            _picture.localRotation = Quaternion.identity;
            _title.alpha = 1f;
            _title.ForceMeshUpdate();
            _reward.localScale = Vector3.one;
            _continue.transform.localScale = Vector3.one;
            SetContinueLive(true);
        }

        private void SetDim(float t)
        {
            Color colour = _dim.color;
            colour.a = _dimAlpha * t;
            _dim.color = colour;
        }

        // Scale 0.1 → 1.15 → 1 and rotation −10° → 3° → 0°: an overshoot, then settling.
        private void PictureStep(float u)
        {
            float scale;
            float angle;
            if (u < PictureOvershootAt)
            {
                float k = DOVirtual.EasedValue(0f, 1f, u / PictureOvershootAt, Ease.OutQuad);
                scale = Mathf.LerpUnclamped(0.1f, 1.15f, k);
                angle = Mathf.LerpUnclamped(-10f, 3f, k);
            }
            else
            {
                float k = DOVirtual.EasedValue(0f, 1f, (u - PictureOvershootAt) / (1f - PictureOvershootAt), Ease.InOutSine);
                scale = Mathf.LerpUnclamped(1.15f, 1f, k);
                angle = Mathf.LerpUnclamped(3f, 0f, k);
            }

            _picture.localScale = new Vector3(scale, scale, 1f);
            _picture.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        /// <summary>
        /// Each visible letter grows from nothing (OutBack) about its own centre, one stagger after the
        /// one before; <paramref name="t"/> is the time since the card started.
        /// </summary>
        private void LetterStep(float t)
        {
            _title.alpha = 1f;
            _title.ForceMeshUpdate();
            TMP_TextInfo info = _title.textInfo;
            int letter = 0;
            for (int i = 0; i < info.characterCount; i++)
            {
                TMP_CharacterInfo character = info.characterInfo[i];
                if (!character.isVisible)
                {
                    continue;
                }

                float local = (t - _animation.WinTitleStart - letter * _animation.WinLetterStagger) / _animation.WinLetterDuration;
                letter++;
                float scale = local <= 0f ? 0f : local >= 1f ? 1f : DOVirtual.EasedValue(0f, 1f, local, Ease.OutBack);
                Vector3[] vertices = info.meshInfo[character.materialReferenceIndex].vertices;
                int v = character.vertexIndex;
                Vector3 centre = (vertices[v] + vertices[v + 2]) * 0.5f;
                for (int k = 0; k < 4; k++)
                {
                    vertices[v + k] = centre + (vertices[v + k] - centre) * scale;
                }
            }

            _title.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
        }

        private int CountLetters()
        {
            _title.ForceMeshUpdate();
            TMP_TextInfo info = _title.textInfo;
            int letters = 0;
            for (int i = 0; i < info.characterCount; i++)
            {
                if (info.characterInfo[i].isVisible)
                {
                    letters++;
                }
            }

            return letters;
        }

        private void RewardStep(float v) => _reward.localScale = PopScale(v);

        private void ContinueStep(float v) => _continue.transform.localScale = PopScale(v);

        private static Vector3 PopScale(float v)
        {
            float scale = Mathf.LerpUnclamped(PopFrom, 1f, v);
            return new Vector3(scale, scale, 1f);
        }

        // Continue takes taps only once it is in place; until then a tap on it skips like any other.
        private void SetContinueLive(bool live)
        {
            foreach (Graphic graphic in _continueGraphics)
            {
                graphic.raycastTarget = live;
            }
        }

        private void Kill()
        {
            if (_sequence != null && _sequence.IsActive())
            {
                _sequence.Kill();
            }

            _sequence = null;
        }

        private void Awake()
        {
            if (_animation == null || _dim == null || _picture == null || _title == null || _reward == null || _continue == null)
            {
                Debug.LogError($"{name}: every reference must be assigned.", this);
                enabled = false;
                return;
            }

            _dimAlpha = _dim.color.a;
            _continueGraphics = _continue.GetComponentsInChildren<Graphic>(true);
            _dimStep = SetDim;
            _pictureStep = PictureStep;
            _letterStep = LetterStep;
            _rewardStep = RewardStep;
            _continueStep = ContinueStep;
            _finish = Finish;
        }

        // Closing (leaving the level) ends the entrance where it is.
        private void OnDisable() => Kill();
    }
}
