using Content.Client.Interactable.Components;
using Content.Shared.DeepFryer;
using Content.Shared.Stealth.Components;
using Content.Shared.DeepFryer.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client.DeepFryer;

public sealed class BeenFriedSystem : SharedBeenFriedSystem
{
    private static readonly ProtoId<ShaderPrototype> Shader = "Fried";

    [Dependency] private readonly IPrototypeManager _protoMan = default!;

    private ShaderInstance _shader = default!;

    public override void Initialize()
    {
        base.Initialize();

        _shader = _protoMan.Index(Shader).InstanceUnique();

        SubscribeLocalEvent<BeenFriedComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<BeenFriedComponent, ComponentStartup>(OnStartup);
    }

    private void SetShader(EntityUid uid, bool enabled, BeenFriedComponent? component = null, SpriteComponent? sprite = null)
    {
        if (!Resolve(uid, ref component, ref sprite, false))
            return;

        sprite.PostShader = enabled ? _shader : null;
        sprite.GetScreenTexture = enabled;
        sprite.RaiseShaderEvent = enabled;
    }

    private void OnStartup(EntityUid uid, BeenFriedComponent component, ComponentStartup args)
    {
        SetShader(uid, true, component);
    }

    private void OnShutdown(EntityUid uid, BeenFriedComponent component, ComponentShutdown args)
    {
        if (!Terminating(uid))
            SetShader(uid, false, component);
    }
}
