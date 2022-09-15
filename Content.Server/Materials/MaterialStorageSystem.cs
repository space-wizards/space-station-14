using Content.Shared.Materials;
using Content.Shared.Popups;
using Robust.Shared.Player;

namespace Content.Server.Materials;

/// <summary>
/// This handles <see cref="SharedMaterialStorageSystem"/>
/// </summary>
public sealed class MaterialStorageSystem : SharedMaterialStorageSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    protected override void OnFinishInsertMaterialEntity(EntityUid toInsert, MaterialStorageComponent component)
    {
        _audio.PlayPvs(component.InsertingSound, component.Owner);
        _popup.PopupEntity(Loc.GetString("machine-insert-item", ("machine", component.Owner),
            ("item", toInsert)), component.Owner, Filter.Pvs(component.Owner));

        QueueDel(toInsert);
    }
}
