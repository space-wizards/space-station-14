using Content.Server.Administration.Logs;
using Content.Shared.Materials;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Server.Power.Components;
using Content.Server.Construction.Components;
using Content.Server.Stack;
using Content.Shared.Database;

namespace Content.Server.Materials;

/// <summary>
/// This handles <see cref="SharedMaterialStorageSystem"/>
/// </summary>
public sealed class MaterialStorageSystem : SharedMaterialStorageSystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StackSystem _stackSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MaterialStorageComponent, MachineDeconstructedEvent>(OnDeconstructed);
    }

    private void OnDeconstructed(EntityUid uid, MaterialStorageComponent component, MachineDeconstructedEvent args)
    {
        if (!component.DropOnDeconstruct)
            return;

        foreach (var (material, amount) in component.Storage)
        {
            _stackSystem.SpawnMultipleFromMaterial(amount, material, Transform(uid).Coordinates);
        }
    }

    public override bool TryInsertMaterialEntity(EntityUid user, EntityUid toInsert, EntityUid receiver, MaterialStorageComponent? component = null)
    {
        if (!Resolve(receiver, ref component))
            return false;
        if (TryComp<ApcPowerReceiverComponent>(receiver, out var power) && !power.Powered)
            return false;
        if (!base.TryInsertMaterialEntity(user, toInsert, receiver, component))
            return false;
        _audio.PlayPvs(component.InsertingSound, component.Owner);
        _popup.PopupEntity(Loc.GetString("machine-insert-item", ("user", user), ("machine", component.Owner),
            ("item", toInsert)), component.Owner);
        QueueDel(toInsert);

        // Logging
        TryComp<StackComponent>(toInsert, out var stack);
        var count = stack?.Count ?? 1;
        _adminLogger.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(user):player} inserted {count} {ToPrettyString(toInsert):inserted} into {ToPrettyString(receiver):receiver}");
        return true;
    }
}
