using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Content.Shared.Humanoid;
using Content.Shared.Patron;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using static Robust.Shared.GameObjects.SharedSpriteComponent;


namespace Content.Client.Patron
{
    public sealed class PatronSystem : SharedPatronSystem
    {
        [Dependency] private readonly IResourceCache _cache = default!;


        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PatronEarsVisualizerComponent, ComponentInit>(OnEarsInitialize);
            SubscribeLocalEvent<PatronEarsVisualizerComponent, OnPatronEarsVisualizerChangedEvent>(OnEarsVisualizerChange);

        }

        private void OnEarsInitialize(EntityUid uid, PatronEarsVisualizerComponent comp, ComponentInit ev)
        {
            SetPlayerEars(comp.Owner, comp.RsiPath);
        }

        private void OnEarsVisualizerChange(EntityUid uid, PatronEarsVisualizerComponent comp, OnPatronEarsVisualizerChangedEvent ev)
        {
            SetPlayerEars(comp.Owner, ev.RsiPath);
        }

        private void SetPlayerEars(EntityUid uid, string path)
        {
            RSI? rsi = _cache.GetResource<RSIResource>(TextureRoot / path).RSI;
            if (rsi is null || rsi.Path is null)
                return;
            if (!TryComp<SpriteComponent>(uid, out var sprite))
                return;
            var layer = new PrototypeLayerData();
            layer.RsiPath = path;
            layer.State = "equipped-HELMET";

            sprite.AddLayer(layer, sprite.LayerMapReserveBlank(HumanoidVisualLayers.HeadTop));
        }
    }
}
