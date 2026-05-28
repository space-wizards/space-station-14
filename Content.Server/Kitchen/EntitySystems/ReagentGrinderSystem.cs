using System.Linq;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.FixedPoint;
using Content.Shared.Kitchen;
using Content.Shared.Kitchen.Components;
using Content.Shared.Kitchen.EntitySystems;
using Content.Shared.Power.EntitySystems;
using Robust.Server.GameObjects;

namespace Content.Server.Kitchen.EntitySystems;

public sealed partial class ReagentGrinderSystem : SharedReagentGrinderSystem
{
    [Dependency] private UserInterfaceSystem _uiSystem = default!;
    [Dependency] private ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private SharedPowerReceiverSystem _power = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;

    public override void UpdateUi(EntityUid uid)
    {
        if (!TryComp<ReagentGrinderComponent>(uid, out var comp))
            return;

        var chamberEntities = comp.InputContainer.ContainedEntities.Select(x => GetNetEntity(x)).ToArray();
        var beaker = _itemSlotsSystem.GetItemOrNull(uid, ReagentGrinderComponent.BeakerSlotId);
        var beakerNet = beaker.HasValue ? GetNetEntity(beaker.Value) : (NetEntity?)null;
        var reagents = new List<ReagentQuantity>();
        FixedPoint2 currentVolume = 0;
        FixedPoint2 maxVolume = 0;

        if (beaker is { } beakerEnt && _solutionContainer.TryGetFitsInDispenser(beakerEnt, out _, out var solution))
        {
            reagents = solution.Contents.ToList();
            currentVolume = solution.Volume;
            maxVolume = solution.MaxVolume;
        }

        var state = new ReagentGrinderUpdateUserInterfaceState(
            chamberEntities,
            beakerNet,
            IsActive((uid, comp)),
            _power.IsPowered(uid),
            comp.Program,
            comp.AutoMode,
            reagents,
            currentVolume,
            maxVolume
        );

        _uiSystem.SetUiState(uid, ReagentGrinderUiKey.Key, state);
    }
}
