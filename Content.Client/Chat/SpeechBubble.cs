using System;
using Content.Client.Interfaces.Chat;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.Client.Chat
{
    public abstract class SpeechBubble : Control
    {
        public enum SpeechType : byte
        {
            Emote,
            Say
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
        private readonly IEntity _senderEntity;
        private readonly IChatManager _chatManager;

        private float _timeLeft = TotalTime;

        public float VerticalOffset { get; set; }
        private float _verticalOffsetAchieved;

        public float ContentHeight { get; private set; }

        public static SpeechBubble CreateSpeechBubble(SpeechType type, string text, IEntity senderEntity, IEyeManager eyeManager, IChatManager chatManager)
        {
            switch (type)
            {
                case SpeechType.Emote:
                    return new EmoteSpeechBubble(text, senderEntity, eyeManager, chatManager);

                case SpeechType.Say:
                    return new SaySpeechBubble(text, senderEntity, eyeManager, chatManager);

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public SpeechBubble(string text, IEntity senderEntity, IEyeManager eyeManager, IChatManager chatManager)
        {
            _chatManager = chatManager;
            _senderEntity = senderEntity;
            _eyeManager = eyeManager;

            // Use text clipping so new messages don't overlap old ones being pushed up.
            RectClipContent = true;

            var bubble = BuildBubble(text);

            AddChild(bubble);

            ForceRunStyleUpdate();

            bubble.Measure(Vector2.Infinity);
            ContentHeight = bubble.DesiredSize.Y;
            _verticalOffsetAchieved = -ContentHeight;
        }

        protected abstract Control BuildBubble(string text);

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);

            _timeLeft -= args.DeltaSeconds;

            if (_timeLeft <= FadeTime)
            {
                // Update alpha if we're fading.
                Modulate = Color.White.WithAlpha(_timeLeft / FadeTime);
            }

            if (_senderEntity.Deleted || _timeLeft <= 0)
            {
                // Timer spawn to prevent concurrent modification exception.
                Timer.Spawn(0, Die);
                return;
            }

            // Lerp to our new vertical offset if it's been modified.
            if (MathHelper.CloseTo(_verticalOffsetAchieved - VerticalOffset, 0, 0.1))
            {
                _verticalOffsetAchieved = VerticalOffset;
            }
            else
            {
                _verticalOffsetAchieved = MathHelper.Lerp(_verticalOffsetAchieved, VerticalOffset, 10 * args.DeltaSeconds);
            }

            if (!_senderEntity.Transform.Coordinates.IsValid(_senderEntity.EntityManager))
                return;

            var worldPos = _senderEntity.Transform.WorldPosition;
            worldPos += (0, EntityVerticalOffset);

            var lowerCenter = _eyeManager.WorldToScreen(worldPos) / UIScale;
            var screenPos = lowerCenter - (Width / 2, ContentHeight + _verticalOffsetAchieved);
            LayoutContainer.SetPosition(this, screenPos);

            var height = MathHelper.Clamp(lowerCenter.Y - screenPos.Y, 0, ContentHeight);
            SetHeight = height;
        }

        private void Die()
        {
            if (Disposed)
            {
                return;
            }

            _chatManager.RemoveSpeechBubble(_senderEntity.Uid, this);
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

    public class EmoteSpeechBubble : SpeechBubble

    {
        public EmoteSpeechBubble(string text, IEntity senderEntity, IEyeManager eyeManager, IChatManager chatManager)
            : base(text, senderEntity, eyeManager, chatManager)
        {
        }

        protected override Control BuildBubble(string text)
        {
            var label = new RichTextLabel
            {
                MaxWidth = 256,
            };
            label.SetMessage(text);

            var panel = new PanelContainer
            {
                StyleClasses = { "speechBox", "emoteBox" },
                Children = { label },
                ModulateSelfOverride = Color.White.WithAlpha(0.75f)
            };

            return panel;
        }
    }

    public class SaySpeechBubble : SpeechBubble
    {
        public SaySpeechBubble(string text, IEntity senderEntity, IEyeManager eyeManager, IChatManager chatManager)
            : base(text, senderEntity, eyeManager, chatManager)
        {
        }

        protected override Control BuildBubble(string text)
        {
            var label = new RichTextLabel
            {
                MaxWidth = 256,
            };
            label.SetMessage(text);

            var panel = new PanelContainer
            {
                StyleClasses = { "speechBox", "sayBox" },
                Children = { label },
                ModulateSelfOverride = Color.White.WithAlpha(0.75f)
            };

            return panel;
        }
    }
}
