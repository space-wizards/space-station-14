using Content.Shared.GameObjects.Components.Mobs;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Graphics.Overlays;
using Robust.Client.Graphics.Shaders;
using Robust.Client.Graphics.Clyde;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using OpenToolkit.Graphics.OpenGL4;
using Content.Shared.Interfaces;
using System.Collections.Generic;
using Robust.Client.Graphics;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.ResourceManagement;
using Robust.Shared.Log;
using Robust.Shared.GameObjects.Components.Renderable;
using System;
using Robust.Shared.Utility;
using Robust.Client.Utility;
using Content.Client.GameObjects.Components.Mobs;
using System.Linq;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.Enums;
using Robust.Client.Interfaces.Graphics;

namespace Content.Client.Graphics.Overlays
{
    public class TextureOverlay : Overlay, IConfigurable<OverlaySpaceOverlayParameter>, IConfigurable<TextureOverlayParameter>, IConfigurable<PositionOverlayParameter>
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IClyde _displayManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        public override OverlaySpace Space => _space;
        public override OverlayPriority Priority => OverlayPriority.P6;

        private readonly ShaderInstance _shader;
        private OverlaySpace _space;
        private Vector2 _currentWorldCoords;
        private KeyValuePair<Texture[], float[]> _textureData;
        private KeyValuePair<string, string> _cachedResourceLocation;
        private AnimState _animData;
        private double _lastFrame;

        public TextureOverlay() : base()
        {
            IoCManager.InjectDependencies(this);
            _shader = _prototypeManager.Index<ShaderPrototype>("Texture").Instance().Duplicate();
        }

        public void Configure(TextureOverlayParameter parameters)
        {
            _lastFrame = _gameTiming.CurTime.TotalSeconds;

            if (parameters.RSIPaths.Length != 1)
            {
                Logger.Error("Error: TextureOverlay only supports a single texture, but {0} RSI paths were provided!", parameters.RSIPaths.Length);
                return;
            }
            if (parameters.States.Length != 1)
            {
                Logger.Error("Error: TextureOverlay only supports a single texture, but {0} states were provided!", parameters.States.Length);
                return;
            }
            if(parameters.RSIPaths[0] != _cachedResourceLocation.Key && parameters.States[0] != _cachedResourceLocation.Value){
                _cachedResourceLocation = new KeyValuePair<string, string>(parameters.RSIPaths[0], parameters.States[0]);
                var specifier = new SpriteSpecifier.Rsi(new ResourcePath(parameters.RSIPaths[0]), parameters.States[0]);
                var textures = SpriteSpecifierExt.FrameArr(specifier);
                var delays = SpriteSpecifierExt.FrameDelays(specifier);
                if (textures.Length < 1)
                {
                    Logger.Error("Error: while loading Texture shader, attempt to load texture at path {0} failed!", parameters.RSIPaths[0]);
                    return;
                }
                if (delays.Length < 1)
                {
                    Logger.Error("Error: while loading Texture shader, attempt to get delays at path {0} failed!", parameters.States[0]);
                    return;
                }
                _textureData = new KeyValuePair<Texture[], float[]>(textures, delays);
                _animData = new AnimState();
            }
        }

        public void Configure(PositionOverlayParameter parameters){
            if (parameters.Positions.Length == 1)
                _currentWorldCoords = parameters.Positions[0];
            else
                Logger.Error("Error: {0} instead of 1 position parameter was sent to TextureOverlay!", parameters.Positions.Length);
        }

        public void Configure(OverlaySpaceOverlayParameter parameters)
        {
            _space = parameters.Space;
        }

        protected override void Draw(DrawingHandleBase handle, OverlaySpace currentSpace)
        {
            handle.UseShader(_shader);
            var worldHandle = (DrawingHandleWorld) handle;
            var viewport = _eyeManager.GetWorldViewport();

            _animData.FrameTime += ((float)_gameTiming.CurTime.TotalSeconds - (float)_lastFrame);
            _lastFrame = _gameTiming.CurTime.TotalSeconds;
            CalculateFrameState();

            var tempCoords = _eyeManager.WorldToScreen(_currentWorldCoords);
            tempCoords.Y = Math.Abs(tempCoords.Y - _displayManager.ScreenSize.Y);
            _shader?.SetParameter("positionInput", tempCoords);
                
            _shader?.SetParameter("tex", _textureData.Key[_animData.Frame]);
            worldHandle.DrawRect(viewport, Color.White);
        }

        private void CalculateFrameState()
        {
            float[] delays = _textureData.Value;
            while(_animData.FrameTime > delays[_animData.Frame])
            {
                if (_animData.Frame == delays.Count()-1) {
                    _animData.FrameTime -= delays[_animData.Frame];
                    _animData.Frame = 0;
                }
                else 
                {
                    _animData.FrameTime -= delays[_animData.Frame];
                    _animData.Frame++;
                }
            }
        }

        private class AnimState {
            public int Frame;
            public float FrameTime;
        }
    }
}
