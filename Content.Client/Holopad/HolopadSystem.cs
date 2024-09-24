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

        SubscribeLocalEvent<HolopadHologramComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<HolopadHologramComponent, BeforePostShaderRenderEvent>(OnShaderRender);

        SubscribeNetworkEvent<HolopadHologramVisualsUpdateEvent>(OnVisualsUpdate);
        SubscribeAllEvent<TypingChangedEvent>(OnTypingChanged);
    }

    private void OnComponentInit(EntityUid uid, HolopadHologramComponent component, ComponentInit ev)
    {
        UpdateColors(uid, component);
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

        var netEv = new HolopadUserTypingChangedEvent(GetNetEntity(uid.Value), ev.IsTyping);
        RaiseNetworkEvent(netEv);
    }

    private void OnVisualsUpdate(HolopadHologramVisualsUpdateEvent ev)
    {
        if (!TryComp<SpriteComponent>(GetEntity(ev.Hologram), out var hologramSprite))
            return;

        if (!TryComp<SpriteComponent>(GetEntity(ev.Target), out var targetSprite))
            return;

        if (!TryComp<HolopadHologramComponent>(GetEntity(ev.Hologram), out var hologramComponent))
            return;

        hologramSprite.CopyFrom(targetSprite);

        // Reset select values
        hologramSprite.Color = Color.White;
        hologramSprite.Offset = hologramComponent.Offset;
        hologramSprite.DrawDepth = (int)DrawDepth.Mobs;

        for (int i = 0; i < hologramSprite.AllLayers.Count(); i++)
        {
            hologramSprite.LayerSetShader(i, "unshaded");
        }

        UpdateColors(GetEntity(ev.Hologram), hologramComponent);
    }

    private void UpdateColors(EntityUid uid, HolopadHologramComponent component)
    {
        if (!_entManager.TryGetComponent<SpriteComponent>(uid, out var sprite))
            return;

        var instance = _prototypeManager.Index<ShaderPrototype>(component.ShaderName).InstanceUnique();
        instance.SetParameter("color1", new Robust.Shared.Maths.Vector3(component.Color1.R, component.Color1.G, component.Color1.B));
        instance.SetParameter("color2", new Robust.Shared.Maths.Vector3(component.Color2.R, component.Color2.G, component.Color2.B));
        instance.SetParameter("alpha", component.Alpha);
        instance.SetParameter("intensity", component.Intensity);

        sprite.PostShader = instance;
        sprite.RaiseShaderEvent = true;
        sprite.Color = Color.White;
        sprite.Offset = new Vector2(-0.03f, 0.45f);
    }
}
