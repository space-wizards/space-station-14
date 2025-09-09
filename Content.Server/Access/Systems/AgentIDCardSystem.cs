using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Robust.Shared.Prototypes;
using Content.Server.Clothing.Systems;
using Content.Server.Implants;
using Content.Shared.Implants;
using Content.Shared.Inventory;
using Content.Shared.PDA;

namespace Content.Server.Access.Systems;

/// <inheritdoc />
public sealed class AgentIDCardSystem : SharedAgentIdCardSystem
{
    [Dependency] private readonly SharedIdCardSystem _cardSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ChameleonClothingSystem _chameleon = default!;
    [Dependency] private readonly ChameleonControllerSystem _chamController = default!;

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AgentIDCardComponent, InventoryRelayedEvent<ChameleonControllerOutfitSelectedEvent>>(OnChameleonControllerOutfitChangedItem);
    }

    private void OnChameleonControllerOutfitChangedItem(Entity<AgentIDCardComponent> ent, ref InventoryRelayedEvent<ChameleonControllerOutfitSelectedEvent> args)
    {
        if (!TryComp<IdCardComponent>(ent, out var idCardComp))
            return;

        _prototypeManager.Resolve(args.Args.ChameleonOutfit.Job, out var jobProto);

        var jobIcon = args.Args.ChameleonOutfit.Icon ?? jobProto?.Icon;
        var jobName = args.Args.ChameleonOutfit.Name ?? jobProto?.Name ?? "";

        if (jobIcon != null)
            _cardSystem.TryChangeJobIcon(ent, _prototypeManager.Index(jobIcon.Value), idCardComp);

        if (jobName != "")
            _cardSystem.TryChangeJobTitle(ent, Loc.GetString(jobName), idCardComp);

        // If you have forced departments use those over the jobs actual departments.
        if (args.Args.ChameleonOutfit?.Departments?.Count > 0)
            _cardSystem.TryChangeJobDepartment(ent, args.Args.ChameleonOutfit.Departments, idCardComp);
        else if (jobProto != null)
            _cardSystem.TryChangeJobDepartment(ent, jobProto, idCardComp);

        // Ensure that you chameleon IDs in PDAs correctly. Yes this is sus...

        // There is one weird interaction: If the job / icon don't match the PDAs job the chameleon will be updated
        // to the PDAs IDs sprite but the icon and job title will not match. There isn't a way to get around this
        // really as there is no tie between job -> pda or pda -> job.

        var idSlotGear = _chamController.GetGearForSlot(args, "id");
        if (idSlotGear == null)
            return;

        var proto = _prototypeManager.Index(idSlotGear);
        if (!proto.TryGetComponent<PdaComponent>(out var comp, EntityManager.ComponentFactory))
            return;

        _chameleon.SetSelectedPrototype(ent, comp.IdCard);
    }
}
