using System.Linq;
using Content.Server.DoAfter;
using Content.Server.Gatherable;
using Content.Server.Gatherable.Components;
using Content.Server.Interaction;
using Content.Server.Mech.Components;
using Content.Server.Mech.Equipment.Components;
using Content.Server.Mech.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Construction.Components;
using Content.Shared.Interaction;
using Content.Shared.Mech;
using Content.Shared.Mech.Equipment.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Wall;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;

namespace Content.Server.Mech.Equipment.EntitySystems;

/// <summary>
/// Handles <see cref="MechDrillComponent"/> and all related UI logic
/// </summary>
public sealed class MechDrillSystem : EntitySystem
{
    [Dependency] private readonly MechSystem _mech = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly InteractionSystem _interaction = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly GatherableSystem _gatherableSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<MechDrillComponent, InteractNoHandEvent>(OnInteract);
        SubscribeLocalEvent<MechDrillComponent, DoAfterEvent>(OnMechDrill);
    }

    private void OnInteract(EntityUid uid, MechDrillComponent component, InteractNoHandEvent args)
    {
        if (args.Handled || args.Target is not {} target)
            return;

        if (!HasComp<GatherableComponent>(target))
        {
            return;
        }

        if (!TryComp<MechComponent>(args.User, out var mech) || mech.PilotSlot.ContainedEntity == target)
            return;

        if (mech.Energy + component.DrillEnergyDelta < 0)
            return;

        if (!_interaction.InRangeUnobstructed(args.User, target))
            return;

        args.Handled = true;
        component.AudioStream = _audio.PlayPvs(component.DrillSound, uid);
        _doAfter.DoAfter(new DoAfterEventArgs(args.User, component.DrillDelay, target:target, used:uid)
        {
            BreakOnTargetMove = true,
            BreakOnUserMove = true
        });

    }

    private void OnMechDrill(EntityUid uid, MechDrillComponent component, DoAfterEvent args)
    {
        if (args.Cancelled)
        {
            component.AudioStream?.Stop();
            return;
        }

        if (args.Handled || args.Args.Target == null)
            return;

        if (!TryComp<MechEquipmentComponent>(uid, out var equipmentComponent) || equipmentComponent.EquipmentOwner == null)
            return;
        if (!_mech.TryChangeEnergy(equipmentComponent.EquipmentOwner.Value, component.DrillEnergyDelta))
            return;

        _mech.UpdateUserInterface(equipmentComponent.EquipmentOwner.Value);

        args.Handled = true;
    }
}
