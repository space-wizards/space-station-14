using Content.Server.Administration.Logs;
using Content.Server.Teleportation.Components;
using Content.Shared.DoAfter;
using Content.Shared.Database;
using Content.Shared.Interaction.Events;
using Content.Shared.Teleportation.Components;
using Content.Shared.Teleportation.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.Teleportation.Systems;

/// <summary>
/// This handles creating portals from a hand teleporter.
/// </summary>
public sealed class DimensionPotSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly LinkedEntitySystem _link = default!;
    [Dependency] private readonly AudioSystem _audio = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
		SubscribeLocalEvent<DimensionPotComponent, ComponentStartup>(DimensionPotStartup);
        SubscribeLocalEvent<DimensionPotComponent, GetVerbsEvent<AlternativeVerb>>(AddTogglePortalVerb);
    }
	
	private void DimensionPotStartup(EntityUid uid, DimensionPotComponent component, ComponentStartup args)
    {
		component.PocketDimensionMap = _mapManager.CreateMap();

		var mapComp = EntityManager.GetComponent<MapComponent>(_mapManager.GetMapEntityId(component.PocketDimensionMap));

		mapComp.Dirty();
    }

    private void AddTogglePortalVerb(EntityUid uid, DimensionPotComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !HasComp<HandsComponent>(args.User))
            return;

        AlternativeVerb verb = new()
        {
            Act = HandleActivation(uid, component, args.User)
        };
        args.Verbs.Add(verb);
    }

    /// <summary>
    ///     Creates or removes the portals to the pocket dimension.
    /// </summary>
    private void HandleActivation(EntityUid uid, HandTeleporterComponent component, EntityUid user)
    {
       
    }
}
