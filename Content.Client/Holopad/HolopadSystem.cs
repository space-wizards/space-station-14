using Content.Shared.Chat.TypingIndicator;
using Content.Shared.Holopad;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System.Linq;
using System.Numerics;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client.Holopad;

public sealed class HolopadSystem : SharedHolopadSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HolopadHologramComponent, BeforePostShaderRenderEvent>(OnShaderRender);
        SubscribeAllEvent<TypingChangedEvent>(OnTypingChanged);
        SubscribeNetworkEvent<HolopadHologramVisualsUpdateEvent>(OnVisualsUpdate);
    }

    private void OnShaderRender(EntityUid uid, HolopadHologramComponent component, BeforePostShaderRenderEvent ev)
    {
        if (ev.Sprite.PostShader == null)
            return;

        ev.Sprite.PostShader.SetParameter("t", (float)_timing.RealTime.TotalSeconds * component.ScrollRate);
    }

    private void OnTypingChanged(TypingChangedEvent ev, EntitySessionEventArgs args)
    {
        var uid = args.SenderSession.AttachedEntity;

        if (!Exists(uid))
            return;

        if (!HasComp<HolopadUserComponent>(uid))
            return;

        var netEv = new HolopadUserTypingChangedEvent(GetNetEntity(uid.Value), ev.IsTyping);
        RaiseNetworkEvent(netEv);
    }

    private void OnVisualsUpdate(HolopadHologramVisualsUpdateEvent ev)
    {
        var hologram = GetEntity(ev.Hologram);
        var target = GetEntity(ev.Target);

        if (!TryComp<SpriteComponent>(hologram, out var hologramSprite))
            return;

        if (!TryComp<HolopadHologramComponent>(hologram, out var holopadhologram))
            return;

        // Mimic the appearance of the target
        if (TryComp<SpriteComponent>(target, out var targetSprite))
        {
            hologramSprite.CopyFrom(targetSprite);

            // Adjust select values
            hologramSprite.Color = Color.White;
            hologramSprite.Offset = holopadhologram.Offset;
            hologramSprite.Scale = new Vector2(1f, 1f);
            hologramSprite.DrawDepth = (int)DrawDepth.Mobs;
            hologramSprite.NoRotation = true;

            for (int i = 0; i < hologramSprite.AllLayers.Count(); i++)
                hologramSprite.LayerSetShader(i, "unshaded");
        }

        // If there's no target, remove all layers and display an 'in-call' symbol instead
        else
        {
            for (int i = hologramSprite.AllLayers.Count() - 1; i >= 0; i--)
                hologramSprite.RemoveLayer(i);

            if (string.IsNullOrEmpty(holopadhologram.RsiPath) || string.IsNullOrEmpty(holopadhologram.RsiState))
                return;

            var layer = new PrototypeLayerData();
            layer.RsiPath = holopadhologram.RsiPath;
            layer.State = holopadhologram.RsiState;
            layer.Shader = "unshaded";

            hologramSprite.AddLayer(layer);
        }

        UpdateShader(GetEntity(ev.Hologram), hologramSprite, holopadhologram);
    }

    private void UpdateShader(EntityUid uid, SpriteComponent sprite, HolopadHologramComponent holopadHologram)
    {
        var instance = _prototypeManager.Index<ShaderPrototype>(holopadHologram.ShaderName).InstanceUnique();
        instance.SetParameter("color1", new Robust.Shared.Maths.Vector3(holopadHologram.Color1.R, holopadHologram.Color1.G, holopadHologram.Color1.B));
        instance.SetParameter("color2", new Robust.Shared.Maths.Vector3(holopadHologram.Color2.R, holopadHologram.Color2.G, holopadHologram.Color2.B));
        instance.SetParameter("alpha", holopadHologram.Alpha);
        instance.SetParameter("intensity", holopadHologram.Intensity);

        sprite.PostShader = instance;
        sprite.RaiseShaderEvent = true;
    }
}
