using Content.Server.Actions;
using Content.Server.IdentityManagement;
using Content.Shared.Changeling.Transform;
using Content.Shared.Forensics;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Prototypes;
using Content.Shared.Speech.Components;
using Content.Shared.Wagging;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects.Components.Localization;

namespace Content.Server.Changeling.Transform;

public sealed class ChangelingTransformSystem : SharedChangelingTransformSystem
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly MetaDataSystem _metaSystem = default!;
    [Dependency] private readonly GrammarSystem _grammarSystem = default!;
    [Dependency] private readonly IdentitySystem _identitySystem = default!;
    [Dependency] private readonly ActionsSystem _actionSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void TransformGrammarSet(EntityUid uid, Gender gender)
    {
        //How to stop quantum gender, a bug where the Examine pronoun will mispredict 100% of the time. Need to SPECIFICALLY
        // also modify the Identities grammar component, before Queuing the identityUpdate
        if(!TryComp<GrammarComponent>(uid, out var currentGrammar))
            return;
        if (!TryComp<IdentityComponent>(uid, out var currentIdentity))
            return;
        var identityContainedUid = currentIdentity!.IdentityEntitySlot.ContainedEntities[0];
        if(!TryComp<GrammarComponent>(identityContainedUid, out var identityGrammar))
            return;
        var grammar = new Entity<GrammarComponent>(uid, currentGrammar);
        var identityGrammarEntity = new Entity<GrammarComponent>(identityContainedUid, identityGrammar!);

        _grammarSystem.SetGender(grammar, gender);
        _grammarSystem.SetGender(identityGrammarEntity, gender);
        _identitySystem.QueueIdentityUpdate(uid);
    }
}

