#nullable enable
using System;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Graphics.Shaders;
using Robust.Client.UserInterface;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.GameObjects.EntitySystems.DoAfter
{
    public sealed class DoAfterBar : Control
    {
        private IGameTiming _gameTiming = default!;
        
        private ShaderInstance _shader;

        /// <summary>
        ///     Set from 0.0f to 1.0f to reflect bar progress
        /// </summary>
        public float Ratio
        {
            get => _ratio;
            set => _ratio = value;
        }

        private float _ratio = 1.0f;

        /// <summary>
        ///     Flash red until removed
        /// </summary>
        public bool Cancelled
        {
            get => _cancelled;
            set
            {
                if (_cancelled == value)
                {
                    return;
                }
                
                _cancelled = value;
                if (_cancelled)
                {
                    _gameTiming = IoCManager.Resolve<IGameTiming>();
                    _lastFlash = _gameTiming.CurTime;
                }   
            }
        }

        private bool _cancelled;

        /// <summary>
        ///     Is the cancellation bar red?
        /// </summary>
        private bool _flash = true;
        
        /// <summary>
        ///     Last time we swapped the flash.
        /// </summary>
        private TimeSpan _lastFlash;
        
        /// <summary>
        ///     How long each cancellation bar flash lasts in seconds.
        /// </summary>
        private const float FlashTime = 0.125f;
        
        private const int XPixelDiff = 20 * DoAfterBarScale;

        public const byte DoAfterBarScale = 2;
        private static readonly Color StartColor = new Color(0.8f, 0.0f, 0.2f); // red
        private static readonly Color EndColor = new Color(0.92f, 0.77f, 0.34f); // yellow
        private static readonly Color CompletedColor = new Color(0.0f, 0.8f, 0.27f); // green

        public DoAfterBar()
        {
            IoCManager.InjectDependencies(this);
            _shader = IoCManager.Resolve<IPrototypeManager>().Index<ShaderPrototype>("unshaded").Instance();
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);
            if (Cancelled)
            {
                if ((_gameTiming.CurTime - _lastFlash).TotalSeconds > FlashTime)
                {
                    _lastFlash = _gameTiming.CurTime;
                    _flash = !_flash;
                }
            } 
        }

        protected override void Draw(DrawingHandleScreen handle)
        {
            base.Draw(handle);

            Color color;

            if (Cancelled)
            {
                if ((_gameTiming.CurTime - _lastFlash).TotalSeconds > FlashTime)
                {
                    _lastFlash = _gameTiming.CurTime;
                    _flash = !_flash;
                }

                color = new Color(1.0f, 0.0f, 0.0f, _flash ? 1.0f : 0.0f);
            } 
            else if (Ratio >= 1.0f)
            {
                color = CompletedColor;
            }
            else
            {
                // lerp
                color = new Color(
                    StartColor.R + (EndColor.R - StartColor.R) * Ratio,
                    StartColor.G + (EndColor.G - StartColor.G) * Ratio,
                    StartColor.B + (EndColor.B - StartColor.B) * Ratio,
                    StartColor.A);
            }
            
            handle.UseShader(_shader);
            // If you want to make this less hard-coded be my guest
            var leftOffset = 2 * DoAfterBarScale;
            var box = new UIBox2i(
                leftOffset, 
                -2 + 2 * DoAfterBarScale,
            leftOffset + (int) (XPixelDiff * Ratio), 
            -2);
            handle.DrawRect(box, color);
        }
    }
}
