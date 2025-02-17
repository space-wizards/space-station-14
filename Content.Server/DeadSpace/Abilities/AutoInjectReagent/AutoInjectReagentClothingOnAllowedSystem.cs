using Content.Shared.DeadSpace.Abilities.AutoInjectReagent.Components;
using Content.Shared.Inventory.Events;

namespace Content.Server.DeadSpace.Abilities.AutoInjectReagentClothingOnAllowed;

public sealed partial class AutoInjectReagentClothingOnAllowedStateSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AutoInjectReagentClothingOnAllowedStateComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<AutoInjectReagentClothingOnAllowedStateComponent, GotUnequippedEvent>(OnUnequipped);
    }

    private void OnEquipped(EntityUid uid, AutoInjectReagentClothingOnAllowedStateComponent component, GotEquippedEvent args)
    {
        var airc = EnsureComp<AutoInjectReagentOnAllowedStateComponent>(args.Equipee);

        airc.AllowedStates = component.AllowedStates;
        airc.Reagents = component.Reagents;
        airc.DurationRegenReagents = component.DurationRegenReagents;
        airc.InjectSound = component.InjectSound;
    }

    private void OnUnequipped(EntityUid uid, AutoInjectReagentClothingOnAllowedStateComponent component, GotUnequippedEvent args)
    {
        RemComp<AutoInjectReagentOnAllowedStateComponent>(args.Equipee);
    }
}
