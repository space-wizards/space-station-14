using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Traits.Assorted;
using Content.Shared.Body.Part;
using Robust.Shared.Network;

namespace Content.Server.Traits.Assorted;

/// <summary>
/// This handles the removal of the hand/hands the user has selected.
/// </summary>
public sealed class AmputeeSystem : EntitySystem
{

    // private bool handRemovedStatus = false;
    // private string handLocation = "";

    public override void Initialize()
    {
        SubscribeLocalEvent<AmputeeComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<AmputeeComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(EntityUid uid, AmputeeComponent component, ComponentStartup args)
    {

    }

    private void OnShutdown(EntityUid uid, AmputeeComponent component, ComponentShutdown args)
    {

    }

    // private void RemoveHands(EntityUid uid, PermanentBlindnessComponent component, RemoveHands args)
    // {
    //
    // }


}
