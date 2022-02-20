using System;
using Content.Client.Chat.Managers;
using Content.Client.Viewport;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.Client.Chat.UI
{
    public abstract class SpeechBubble : Control
    {
        public enum SpeechType : byte
        {
            Emote,
            Say,
            Whisper
        }

        /// <summary>
        ///     The total time a speech bubble stays on screen.
        /// </summary>
        private const float TotalTime = 4;

        /// <summary>
        ///     The amount of time at the end of the bubble's life at which it starts fading.
        /// </summary>
        private const float FadeTime = 0.25f;

        /// <summary>
        ///     The distance in world space to offset the speech bubble from the center of the entity.
        ///     i.e. greater -> higher above the mob's head.
        /// </summary>
        private const float EntityVerticalOffset = 0.5f;

        private readonly IEyeManager _eyeManager;
        private readonly EntityUid _senderEntity;
        private readonly IChatManager _chatManager;
        private readonly IEntityManager _entityManager;

        private float _timeLeft = TotalTime;

        public float VerticalOffset { get; set; }
        private float _verticalOffsetAchieved;

        public float ContentHeight { get; private set; }

        public static SpeechBubble CreateSpeechBubble(SpeechType type, string text, EntityUid senderEntity, IEyeManager eyeManager, IChatManager chatManager, IEntityManager entityManager)
        {
            switch (type)
            {
                case SpeechType.Emote:
                    return new TextSpeechBubble(text, senderEntity, eyeManager, chatManager, entityManager, "emoteBox");

                case SpeechType.Say:
                    return new TextSpeechBubble(text, senderEntity, eyeManager, chatManager, entityManager, "sayBox");

                case SpeechType.Whisper:
                    return new TextSpeechBubble(text, senderEntity, eyeManager, chatManager, entityManager, "whisperBox");

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public SpeechBubble(string text, EntityUid senderEntity, IEyeManager eyeManager, IChatManager chatManager, IEntityManager entityManager, string speechStyleClass)
        {
            _chatManager = chatManager;
            _senderEntity = senderEntity;
            _eyeManager = eyeManager;
            _entityManager = entityManager;

            // Use text clipping so new messages don't overlap old ones being pushed up.
            RectClipContent = true;

            var bubble = BuildBubble(text, speechStyleClass);

            AddChild(bubble);

            ForceRunStyleUpdate();

            bubble.Measure(Vector2.Infinity);
            ContentHeight = bubble.DesiredSize.Y;
            _verticalOffsetAchieved = -ContentHeight;
        }

        protected abstract Control BuildBubble(string text, string speechStyleClass);

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);

            _timeLeft -= args.DeltaSeconds;
            if (_entityManager.Deleted(_senderEntity) || _timeLeft <= 0)
            {
                // Timer spawn to prevent concurrent modification exception.
                Timer.Spawn(0, Die);
                return;
            }

            // Lerp to our new vertical offset if it's been modified.
            if (MathHelper.CloseToPercent(_verticalOffsetAchieved - VerticalOffset, 0, 0.1))
            {
                _verticalOffsetAchieved = VerticalOffset;
            }
            else
            {
                _verticalOffsetAchieved = MathHelper.Lerp(_verticalOffsetAchieved, VerticalOffset, 10 * args.DeltaSeconds);
            }

            if (!_entityManager.TryGetComponent<TransformComponent>(_senderEntity, out var xform)
                    || !xform.Coordinates.IsValid(_entityManager))
            {
                Modulate = Color.White.WithAlpha(0);
                return;
            }

            if (_timeLeft <= FadeTime)
            {
                // Update alpha if we're fading.
                Modulate = Color.White.WithAlpha(_timeLeft / FadeTime);
            }
            else
            {
                // Make opaque otherwise, because it might have been hidden before
                Modulate = Color.White;
            }


            var worldPos = xform.WorldPosition;
            var scale = _eyeManager.MainViewport.GetRenderScale();
            var offset = new Vector2(0, EntityVerticalOffset * EyeManager.PixelsPerMeter * scale);
            var lowerCenter = (_eyeManager.WorldToScreen(worldPos) - offset) / UIScale;

            var screenPos = lowerCenter - (Width / 2, ContentHeight + _verticalOffsetAchieved);
            // Round to nearest 0.5
            screenPos = (screenPos * 2).Rounded() / 2;
            LayoutContainer.SetPosition(this, screenPos);

            var height = MathF.Ceiling(MathHelper.Clamp(lowerCenter.Y - screenPos.Y, 0, ContentHeight));
            SetHeight = height;
        }

        private void Die()
        {
            if (Disposed)
            {
                return;
            }

            _chatManager.RemoveSpeechBubble(_senderEntity, this);
        }

        /// <summary>
        ///     Causes the speech bubble to start fading IMMEDIATELY.
        /// </summary>
        public void FadeNow()
        {
            if (_timeLeft > FadeTime)
            {
                _timeLeft = FadeTime;
            }
        }
    }

    public sealed class TextSpeechBubble : SpeechBubble

    {
        public TextSpeechBubble(string text, EntityUid senderEntity, IEyeManager eyeManager, IChatManager chatManager, IEntityManager entityManager, string speechStyleClass)
            : base(text, senderEntity, eyeManager, chatManager, entityManager, speechStyleClass)
        {
        }

        protected override Control BuildBubble(string text, string speechStyleClass)
        {
            var label = new RichTextLabel
            {
                MaxWidth = 256,
            };
            label.SetMessage(text);

            var panel = new PanelContainer
            {
                StyleClasses = { "speechBox", speechStyleClass },
                Children = { label },
                ModulateSelfOverride = Color.White.WithAlpha(0.75f)
            };

            return panel;
        }
    }
}
