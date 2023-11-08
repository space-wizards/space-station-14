using System.Numerics;
using Content.Client.Chat.Managers;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Chat.UI
{
    public abstract class SpeechBubble : Control
    {
        public enum SpeechType : byte
        {
            Emote,
            Say,
            Whisper,
            Looc
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

        public Vector2 ContentSize { get; private set; }

        // man down
        public event Action<EntityUid, SpeechBubble>? OnDied;

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

                case SpeechType.Looc:
                    return new TextSpeechBubble(text, senderEntity, eyeManager, chatManager, entityManager, "emoteBox", Color.FromHex("#48d1cc"));

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public SpeechBubble(string text, EntityUid senderEntity, IEyeManager eyeManager, IChatManager chatManager, IEntityManager entityManager, string speechStyleClass, Color? fontColor = null)
        {
            _chatManager = chatManager;
            _senderEntity = senderEntity;
            _eyeManager = eyeManager;
            _entityManager = entityManager;

            // Use text clipping so new messages don't overlap old ones being pushed up.
            RectClipContent = true;

            var bubble = BuildBubble(text, speechStyleClass, fontColor);

            AddChild(bubble);

            ForceRunStyleUpdate();

            bubble.Measure(Vector2Helpers.Infinity);
            ContentSize = bubble.DesiredSize;
            _verticalOffsetAchieved = -ContentSize.Y;
        }

        protected abstract Control BuildBubble(string text, string speechStyleClass, Color? fontColor = null);

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

            if (!_entityManager.TryGetComponent<TransformComponent>(_senderEntity, out var xform) || xform.MapID != _eyeManager.CurrentMap)
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

            var offset = (-_eyeManager.CurrentEye.Rotation).ToWorldVec() * -EntityVerticalOffset;
            var worldPos = xform.WorldPosition + offset;

            var lowerCenter = _eyeManager.WorldToScreen(worldPos) / UIScale;
            var screenPos = lowerCenter - new Vector2(ContentSize.X / 2, ContentSize.Y + _verticalOffsetAchieved);
            // Round to nearest 0.5
            screenPos = (screenPos * 2).Rounded() / 2;
            LayoutContainer.SetPosition(this, screenPos);

            var height = MathF.Ceiling(MathHelper.Clamp(lowerCenter.Y - screenPos.Y, 0, ContentSize.Y));
            SetHeight = height;
        }

        private void Die()
        {
            if (Disposed)
            {
                return;
            }

            OnDied?.Invoke(_senderEntity, this);
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
        public TextSpeechBubble(string text, EntityUid senderEntity, IEyeManager eyeManager, IChatManager chatManager, IEntityManager entityManager, string speechStyleClass, Color? fontColor = null)
            : base(text, senderEntity, eyeManager, chatManager, entityManager, speechStyleClass, fontColor)
        {
        }

        protected override Control BuildBubble(string text, string speechStyleClass, Color? fontColor = null)
        {
            var label = new RichTextLabel
            {
                MaxWidth = 256,
            };

            if (fontColor != null)
            {
                var msg = new FormattedMessage();
                msg.PushColor(fontColor.Value);
                msg.AddMarkup(text);
                label.SetMessage(msg);
            }
            else
            {
                label.SetMessage(text);
            }

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
