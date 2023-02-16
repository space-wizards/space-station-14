using Content.Client.Interactable.Components;
using Content.Shared.Inventory.Events;
using Content.Shared.Ninja.Components;
using Content.Shared.Ninja.Systems;
using Content.Shared.Stealth.Components;

namespace Content.Client.Ninja;

public sealed class NinjaSystem : SharedNinjaSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpaceNinjaSuitComponent, GotUnequippedEvent>(OnSuitUnequipped);
    }

    private void OnSuitUnequipped(EntityUid uid, SpaceNinjaSuitComponent comp, GotUnequippedEvent args)
    {
        var user = args.Equipee;

        // force uncloak
        comp.Cloaked = false;
        SetCloaked(user, false);

        // prevent funny crash when removing StealthComponent
        RemComp<InteractionOutlineComponent>(user);
        RemCompDeferred<StealthComponent>(user);
    }
}
