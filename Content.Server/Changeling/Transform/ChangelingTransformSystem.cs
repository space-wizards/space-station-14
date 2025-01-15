using Content.Server.Actions;
using Content.Server.Emoting.Components;
using Content.Server.Emoting.Systems;
using Content.Server.IdentityManagement;
using Content.Shared.Changeling.Devour;
using Content.Shared.Changeling.Transform;
using Content.Shared.Forensics;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Prototypes;
using Content.Shared.Speech.Components;
using Content.Shared.Wagging;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects.Components.Localization;

namespace Content.Server.Changeling.Transform;

public sealed class ChangelingTransformSystem : SharedChangelingTransformSystem
{
    [Dependency] private readonly GrammarSystem _grammarSystem = default!;
    [Dependency] private readonly IdentitySystem _identitySystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly BodyEmotesSystem _bodyEmotesSystem = default!;


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

    protected override void TransformBodyEmotes(EntityUid uid, EntityUid target)
    {
        if (!TryComp<BodyEmotesComponent>(target, out var targetBodyEmotes)
            || !TryComp<BodyEmotesComponent>(uid, out var existingBodyEmotes))
            return;

        BodyEmotesSystem.Replace(existingBodyEmotes, targetBodyEmotes);

    }
    protected override void StartSound(EntityUid uid, ChangelingTransformComponent component, SoundSpecifier? sound)
    {
        if(sound is not null)
            component.CurrentTransformSound = _audioSystem.PlayPvs(sound, uid)!.Value.Entity;
    }

    protected override void StopSound(EntityUid uid, ChangelingTransformComponent component)
    {
        if (component.CurrentTransformSound is not null)
            _audioSystem.Stop(component.CurrentTransformSound);
        component.CurrentTransformSound = null;
    }
}

