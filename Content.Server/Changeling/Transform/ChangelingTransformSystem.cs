using Content.Server.Cloning;
using Content.Server.Configurable;
using Content.Server.Emoting.Components;
using Content.Server.IdentityManagement;
using Content.Shared.Changeling.Transform;
using Content.Shared.Cloning;
using Content.Shared.IdentityManagement.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.Prototypes;

namespace Content.Server.Changeling.Transform;

public sealed partial class ChangelingTransformSystem : SharedChangelingTransformSystem
{
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly CloningSystem _cloningSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    protected override void ApplyComponentChanges(EntityUid ent, EntityUid target, ProtoId<CloningSettingsPrototype> settingsId)
    {
        if (!_prototype.TryIndex(settingsId, out var settings))
            return;
        _cloningSystem.CloneComponents(ent, target, settings);
    }
}

