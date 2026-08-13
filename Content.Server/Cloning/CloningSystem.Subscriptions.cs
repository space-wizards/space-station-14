using System.Diagnostics.CodeAnalysis;
using Content.Server.Zombies;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Cloning;
using Content.Shared.Cloning.Events;
using Content.Shared.Inventory;
using Content.Shared.Metabolism;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Speech.Components;
using Content.Shared.Speech.EntitySystems;
using Content.Shared.Storage;
using Content.Shared.Zombies;
using Robust.Server.GameObjects;

namespace Content.Server.Cloning;

/// <summary>
/// The part of item cloning responsible for copying over important components.
/// </summary>
/// <remarks>
/// This is separate from the CloningSystem to place cloning logic closer together.
/// To exclude or add any specific copy code, place this in cloning context.
/// </remarks>
public sealed partial class CloningSystem
{
    [Dependency] private BloodstreamSystem _bloodstream = default!;
    [Dependency] private MetabolizerSystem _metabolizer = default!;
    [Dependency] private MovementSpeedModifierSystem _moveSpeedMod = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private VocalSystem _vocal = default!;
    [Dependency] private ZombieSystem _zombie = default!;

    // Keep these alphabetized!
    #region Event Handlers
    [SubscribeLocalEvent]
    private void OnBloodstreamCloned(Entity<BloodstreamComponent> ent, ref ClonedEvent args)
    {
        if (!Copied<BloodstreamComponent>(args.CloneUid, args.Settings, out var cloneComp))
            return;

        _bloodstream.ChangeBloodReagents((args.CloneUid, cloneComp), ent.Comp.BloodReferenceSolution);
        _metabolizer.UpdateMetabolicMultiplier(args.CloneUid);
    }

    [SubscribeLocalEvent]
    private void OnInventoryCloned(Entity<InventoryComponent> ent, ref ClonedEvent args)
    {
        if (!Copied<InventoryComponent>(args.CloneUid, args.Settings, out var cloneComp))
            return;

        _inventory.UpdateInventoryTemplate((args.CloneUid, cloneComp));
    }

    [SubscribeLocalEvent]
    private void OnMovementSpeedModsCloned(Entity<MovementSpeedModifierComponent> ent, ref ClonedEvent args)
    {
        if (!Copied<MovementSpeedModifierComponent>(args.CloneUid, args.Settings, out var cloneComp))
            return;

        _moveSpeedMod.RefreshWeightlessModifiers(args.CloneUid);
        _moveSpeedMod.RefreshMovementSpeedModifiers(args.CloneUid);
        _moveSpeedMod.RefreshFrictionModifiers(args.CloneUid);
    }

    [SubscribeLocalEvent]
    private void OnStorageCloned(Entity<StorageComponent> ent, ref ClonedEvent args)
    {
        if (!Copied<StorageComponent>(args.CloneUid, args.Settings, out var cloneComp))
            return;

        _storage.UpdateOccupied((args.CloneUid, cloneComp));

        var cloneUi = EnsureComp<UserInterfaceComponent>(args.CloneUid);
        _ui.SetUi((args.CloneUid, cloneUi), StorageComponent.StorageUiKey.Key, new InterfaceData("StorageBoundUserInterface"));
    }

    [SubscribeLocalEvent]
    private void OnVocalCloned(Entity<VocalComponent> ent, ref ClonedEvent args)
    {
        if (!Copied<VocalComponent>(args.CloneUid, args.Settings, out var cloneComp))
            return;

        _vocal.LoadSounds((args.CloneUid, cloneComp), cloneComp.EmoteSounds);
    }

    [SubscribeLocalEvent]
    private void OnZombieCloned(Entity<ZombieComponent> ent, ref ClonedEvent args)
    {
        // Return the original's appearance to how it was before being zombified.
        _zombie.UnZombify(ent, args.CloneUid, ent.Comp);
    }
    #endregion Event Handlers

    /// <summary>
    /// Checks if a target entity has had the given component copied over to it, returning it into <paramref name="component"/>
    /// </summary>
    private bool Copied<T>(EntityUid target, CloningSettingsPrototype cloneSettings, [NotNullWhen(true)] out T? component) where T : Component
    {
        component = null;
        if (!cloneSettings.Components.Contains(Factory.GetRegistration(typeof(T)).Name))
            return false;

        return Resolve(target, ref component);
    }
}
