using Content.Shared.Holopad;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Client.Holopad;

public sealed class HolopadSystem : SharedHolopadSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HolopadHologramComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<HolopadHologramComponent, BeforePostShaderRenderEvent>(OnShaderRender);
    }

    private void OnComponentInit(EntityUid uid, HolopadHologramComponent component, ComponentInit ev)
    {
        if (!_entManager.TryGetComponent<SpriteComponent>(uid, out var sprite))
            return;

        var instance = _prototypeManager.Index<ShaderPrototype>(component.ShaderName).InstanceUnique();
        instance.SetParameter("color1", new Vector3(component.Color1.R, component.Color1.G, component.Color1.B));
        instance.SetParameter("color2", new Vector3(component.Color2.R, component.Color2.G, component.Color2.B));
        instance.SetParameter("alpha", component.Alpha);
        instance.SetParameter("intensity", component.Intensity);

        sprite.PostShader = instance;
        sprite.RaiseShaderEvent = true;
        sprite.Color = Color.White;
    }

    private void OnShaderRender(EntityUid uid, HolopadHologramComponent component, BeforePostShaderRenderEvent ev)
    {
        if (ev.Sprite.PostShader == null)
            return;

        ev.Sprite.PostShader.SetParameter("t", (float)_timing.RealTime.TotalSeconds * component.ScrollRate);
    }
}
