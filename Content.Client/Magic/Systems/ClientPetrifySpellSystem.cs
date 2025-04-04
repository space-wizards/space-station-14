using Content.Shared.Magic.Components;
using Content.Shared.Magic.Systems;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client.Magic.Systems;

public sealed class ClientPetrifySpellSystem : PetrifySpellSystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;

    private ShaderInstance _shader = default!;

    public override void Initialize()
    {
        base.Initialize();

        _shader = _protoMan.Index<ShaderPrototype>("Greyscale").InstanceUnique();
    }

    protected override void OnStartup(EntityUid ent, PetrifiedStatueComponent comp, ComponentStartup args)
    {
        base.OnStartup(ent, comp, args);

        SetShader(ent, true);
    }

    private void SetShader(EntityUid ent, bool enabled, SpriteComponent? sprite = null)
    {
        if (!Resolve(ent, ref sprite, false))
            return;

        sprite.PostShader = enabled ? _shader : null;
        sprite.GetScreenTexture = enabled;
    }
}
