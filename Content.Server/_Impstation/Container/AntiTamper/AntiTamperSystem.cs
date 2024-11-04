using Content.Server.Sound;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Audio;
using Content.Shared.Destructible;
using Content.Shared.Lock;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;

namespace Content.Server._Impstation.Container.AntiTamper;

public sealed partial class AntiTamperSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly LockSystem _lockSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AntiTamperComponent, DestructionEventArgs>(OnDestroy, before: [typeof(EntityStorageSystem)]);
    }

    private void OnDestroy(EntityUid uid, AntiTamperComponent comp, DestructionEventArgs args)
    {
        if (!TryComp<ContainerManagerComponent>(uid, out var containerManager))
            return;

        if (!_lockSystem.IsLocked(uid))
            return;

        foreach (var container in _containerSystem.GetAllContainers(uid, containerManager))
        {
            if (comp.Containers != null && !comp.Containers.Contains(container.ID))
                continue;

            _containerSystem.CleanContainer(container);
        }

        var coords = Transform(uid).Coordinates;

        _popupSystem.PopupCoordinates(Loc.GetString(comp.Message, ("container", uid)), coords, PopupType.SmallCaution);
        _audioSystem.PlayPvs(comp.Sound, coords);
    }
}
