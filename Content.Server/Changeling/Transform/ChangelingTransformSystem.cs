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

public sealed class ChangelingTransformSystem : SharedChangelingTransformSystem
{
    [Dependency] private readonly GrammarSystem _grammarSystem = default!;
    [Dependency] private readonly IdentitySystem _identitySystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly CloningSystem _cloningSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void TransformGrammarSet(EntityUid uid, Gender gender)
    {
        //How to stop quantum gender, a bug where the Examine pronoun will mispredict 100% of the time. Need to SPECIFICALLY
        // also modify the Identities grammar component, before Queuing the identityUpdate
        if(!TryComp<GrammarComponent>(uid, out var currentGrammar)
           || !TryComp<IdentityComponent>(uid, out var currentIdentity))
            return;

        var identityContainedUid = currentIdentity!.IdentityEntitySlot.ContainedEntities[0]; // get the IdentityComponent so we can modify that too

        if(!TryComp<GrammarComponent>(identityContainedUid, out var identityGrammar))
            return;

        var grammar = new Entity<GrammarComponent>(uid, currentGrammar);
        var identityGrammarEntity = new Entity<GrammarComponent>(identityContainedUid, identityGrammar!);

        _grammarSystem.SetGender(grammar, gender);
        _grammarSystem.SetGender(identityGrammarEntity, gender);
        _identitySystem.QueueIdentityUpdate(uid);
    }

    protected override void StartSound(Entity<ChangelingTransformComponent> ent, SoundSpecifier? sound)
    {
        if(sound is not null)
            ent.Comp.CurrentTransformSound = _audioSystem.PlayPvs(sound, ent)!.Value.Entity;
    }

    protected override void StopSound(Entity<ChangelingTransformComponent> ent)
    {
        if (ent.Comp.CurrentTransformSound is not null)
            _audioSystem.Stop(ent.Comp.CurrentTransformSound);

        ent.Comp.CurrentTransformSound = null;
    }

    protected override void ApplyComponentChanges(EntityUid ent, EntityUid target, ProtoId<CloningSettingsPrototype> settingsId)
    {
        if (!_prototype.TryIndex(settingsId, out var settings))
            return;
        _cloningSystem.CloneComponents(ent, target, settings);
    }
}

