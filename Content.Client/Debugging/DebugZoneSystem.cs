using Content.Shared.Atmos;
using NJsonSchema.Validation;
using Robust.Client.Graphics;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Graphics.Overlays;
using Robust.Client.Graphics.Shaders;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Client.Interfaces.Graphics.Overlays;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.Interfaces.UserInterface;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Content.Client.Debugging
{
    /// <summary>
    /// System which draws atmosphere zone debug overlays for the client.
    /// </summary>
    /// <remarks>
    /// Connects to the <see cref="EntitySystem"/> of the same name in <see cref="Content.Server"/>.
    /// </remarks>
    class DebugZoneSystem : EntitySystem
    {
#pragma warning disable 649
        [Dependency] private readonly IOverlayManager _overlayManager;
        [Dependency] private readonly IEyeManager _eyeManager;
        [Dependency] private readonly IMapManager _mapManager;
        [Dependency] private readonly IPrototypeManager _prototypeManager;
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager;
#pragma warning restore 649

        private Font _font;

        /// <summary>
        /// The current zone being drawn.
        /// </summary>
        public ZoneInfo CurrentZone { get; private set; }

        private bool _drawZones;

        /// <summary>
        /// Should the zone debug overlays be drawn.
        /// </summary>
        public bool DrawZones
        {
            get => _drawZones;
            set
            {
                _drawZones = value;
                if (value)
                {
                    _overlayManager.AddOverlay(new ZoneOverlay(this));
                }
                else
                {
                    _overlayManager.RemoveOverlay(nameof(ZoneOverlay));
                }
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<ZoneInfo>(UpdateZoneOverlay);

            _font = new VectorFont(IoCManager.Resolve<IResourceCache>().GetResource<FontResource>("/Textures/Interface/Nano/NotoSans/NotoSans-Regular.ttf"), 10);
        }

        private void UpdateZoneOverlay(ZoneInfo zoneInfo)
        {
            if (!_drawZones)
                return;

            CurrentZone = zoneInfo;
        }

        /// <summary>
        /// The client overlay which actually draws zone debug information.
        /// </summary>
        private class ZoneOverlay : Overlay
        {
            private DebugZoneSystem _parent;
            public override OverlaySpace Space => OverlaySpace.ScreenSpace;

            public ZoneOverlay(DebugZoneSystem parent) : base(nameof(ZoneOverlay))
            {
                _parent = parent;

                Shader = _parent._prototypeManager.Index<ShaderPrototype>("unshaded").Instance();
            }

            protected override void Draw(DrawingHandleBase handle)
            {
                if (_parent.CurrentZone == null)
                    return;

                var screenHandle = (DrawingHandleScreen) handle;

                // We should never have a zone to draw if the eye itself is not on any grid
                if (!_parent._mapManager.TryFindGridAt(_parent._eyeManager.CurrentEye.Position, out var grid))
                    throw new NotImplementedException(); // TODO: better exception

                // Draw a red box over every distinct coordinate in the zone
                foreach (var coordinate in _parent.CurrentZone.Cells)
                {
                    var tile = grid.GetTileRef(coordinate);

                    var tileBase = new Vector2(tile.X, tile.Y);

                    var screenCorner1 = _parent._eyeManager.WorldToScreen(tileBase);
                    var screenCorner2 = _parent._eyeManager.WorldToScreen(tileBase + (1f, 1f));

                    var tileBox = new UIBox2(screenCorner1, screenCorner2);

                    screenHandle.DrawRect(tileBox, Color.Red.WithAlpha(64), filled: true);
                }

                var lineHeight = _parent._font.GetLineHeight(_parent._userInterfaceManager.UIScale);

                var leftMost = _parent.CurrentZone.Cells.Aggregate((a, b) => b.X < a.X || b.Y > a.Y ? b : a);
                var leftMostTile = grid.GetTileRef(leftMost);

                // Write to the top-left of the chosen tile, but still inside it
                var textPosition = _parent._eyeManager.WorldToScreen(new Vector2(leftMostTile.X, leftMostTile.Y + 1)) + (0, lineHeight);

                Vector2 WriteLine(Vector2 textPosition, string s)
                {
                    var charPosition = textPosition;

                    foreach (var c in s)
                    {
                        var advance = _parent._font.DrawChar(screenHandle, c, charPosition, _parent._userInterfaceManager.UIScale, Color.White);

                        charPosition += new Vector2(advance, 0);
                    }

                    return textPosition + new Vector2(0, lineHeight);
                }

                textPosition = WriteLine(textPosition, _parent.CurrentZone.Cells.Length.ToString());

                foreach (var gas in _parent.CurrentZone.Contents)
                {
                    var gasInfo = $"{gas.GasId}: {gas.Quantity} mols, {gas.PartialPressure} kPa";
                    textPosition = WriteLine(textPosition, gasInfo);
                }
            }
        }
    }
}
