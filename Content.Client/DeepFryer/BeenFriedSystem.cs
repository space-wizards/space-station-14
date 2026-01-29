using Content.Shared.DeepFryer;
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

        SubscribeLocalEvent<BeenFriedComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<BeenFriedComponent, ComponentShutdown>(OnShutdown);
    }

    private void SetShader(Entity<BeenFriedComponent?, SpriteComponent?> ent, bool enabled)
    {
        if (!Resolve(ent.Owner, ref ent.Comp1, ref ent.Comp2, false))
            return;

        ent.Comp2.PostShader = enabled ? _shader : null;
        ent.Comp2.GetScreenTexture = enabled;
        ent.Comp2.RaiseShaderEvent = enabled;
    }

    private void OnStartup(Entity<BeenFriedComponent> ent, ref ComponentStartup args)
    {
        SetShader(ent.AsNullable(), true);
    }

    private void OnShutdown(Entity<BeenFriedComponent> ent, ref ComponentShutdown args)
    {
        if (!Terminating(ent.Owner))
            SetShader(ent.AsNullable(), false);
    }
}
