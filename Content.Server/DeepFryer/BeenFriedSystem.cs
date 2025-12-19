using Content.Server.Access.Components;
using Content.Shared.Access.Components;
using Content.Shared.DeepFryer;
using Content.Shared.DeepFryer.Components;
using Content.Shared.NameModifier.EntitySystems;


namespace Content.Server.DeepFryer;

public sealed class BeenFriedSystem : SharedBeenFriedSystem
{
    [Dependency] private readonly NameModifierSystem _nameMod = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BeenFriedComponent, ComponentInit>(OnInit);
    }

    private void OnInit(EntityUid uid, BeenFriedComponent component, ComponentInit args)
    {
        _nameMod.RefreshNameModifiers(uid);
        // ID cards brick outright
        RemComp<AccessComponent>(uid);
        RemComp<IdCardComponent>(uid);
        RemComp<PresetIdCardComponent>(uid);
        RemComp<AgentIDCardComponent>(uid);
    }
}
