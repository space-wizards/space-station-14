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

    private void OnInit(Entity<BeenFriedComponent> ent, ref ComponentInit args)
    {
        _nameMod.RefreshNameModifiers(ent.Owner);
        // ID cards are completely bricked when they are fried
        RemComp<AccessComponent>(ent.Owner);
        RemComp<IdCardComponent>(ent.Owner);
        RemComp<PresetIdCardComponent>(ent.Owner);
        RemComp<AgentIDCardComponent>(ent.Owner);
    }
}
