using Content.Shared.Materials;
using Content.Shared.Popups;
using Content.Server.Power.Components;
using Robust.Shared.Player;

namespace Content.Server.Materials;

/// <summary>
/// This handles <see cref="SharedMaterialStorageSystem"/>
/// </summary>
public sealed class MaterialStorageSystem : SharedMaterialStorageSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

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
            ("item", toInsert)), component.Owner, Filter.Pvs(component.Owner));
        QueueDel(toInsert);
        return true;
    }
}
