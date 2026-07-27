using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Verbs;
using Robust.Shared.Containers;

namespace Content.Shared.Weapons.Ranged.Systems;

public sealed partial class FingerGunsSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _containers = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FingerGunsComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<FingerGunsComponent, UseInHandEvent>(OnActivate, before: new[] { typeof(ClothingSystem) });
        SubscribeLocalEvent<FingerGunsGunComponent, GetVerbsEvent<AlternativeVerb>>(OnGunGetVerbs);
    }

    private void OnMapInit(Entity<FingerGunsComponent> ent, ref MapInitEvent args)
    {
        PredictedTrySpawnInContainer(ent.Comp.GunPrototype, ent, ent.Comp.ContainerId, out _);
    }

    private void OnActivate(Entity<FingerGunsComponent> ent, ref UseInHandEvent args)
    {
        args.Handled = true; // prevents using in hand from trying to equip it to hands slot by default

        if (!_containers.TryGetContainer(ent, ent.Comp.ContainerId, out var container) || container.ContainedEntities.Count == 0)
            return;

        var gun = container.ContainedEntities[0];
        if (!TryComp<FingerGunsGunComponent>(gun, out var gunComp))
            return;

        SwapForm(ent, gun, gunComp.ContainerId);
    }

    private void OnGunGetVerbs(Entity<FingerGunsGunComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (!_containers.TryGetContainer(ent, ent.Comp.ContainerId, out var container) || container.ContainedEntities.Count == 0)
            return;

        var glove = container.ContainedEntities[0];
        if (!TryComp<FingerGunsComponent>(glove, out var gloveComp))
            return;

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("finger-guns-revert"),
            Act = () => SwapForm(ent, glove, gloveComp.ContainerId),
        });
    }

    /// <summary>
    /// Swaps the active version for the item stashed inside it, placing the stashed item
    /// in the hand the active one was, and stashing the original item inside the new item that's now in hand
    /// (does that make sense? Takes X out of Y and puts Y into X, and vice versa)
    /// Used to move betwen the gloves and gun forms without deleting or recreating either entity
    /// </summary>
    private void SwapForm(EntityUid visible, EntityUid hidden, string stashContainerIdOnHidden)
    {
        if (!_containers.TryGetContainingContainer((visible, null, null), out var handContainer) ||
            !_containers.TryGetContainingContainer((hidden, null, null), out var stashOnVisible) ||
            !_containers.TryGetContainer(hidden, stashContainerIdOnHidden, out var stashOnHidden))
            return;

        _containers.Remove(hidden, stashOnVisible, reparent: false);
        _containers.Remove(visible, handContainer, reparent: false);
        _containers.Insert(hidden, handContainer);
        _containers.Insert(visible, stashOnHidden);
    }
}
