using Content.Server.Cloning;
using Content.Shared.Changeling.Transform;
using Content.Shared.Cloning;
using Robust.Shared.Prototypes;

namespace Content.Server.Changeling.Transform;

public sealed partial class ChangelingTransformSystem : SharedChangelingTransformSystem
{
    [Dependency] private readonly CloningSystem _cloningSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    protected override void ApplyComponentChanges(EntityUid ent, EntityUid target, ProtoId<CloningSettingsPrototype> settingsId)
    {
        if (!_prototype.Resolve(settingsId, out var settings))
            return;

        _cloningSystem.CloneComponents(ent, target, settings);
    }
}

