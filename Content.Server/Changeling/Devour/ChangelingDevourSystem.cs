using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.Changeling.Devour;
using Content.Shared.Devour.Components;
using Content.Shared.Devour;
using Content.Shared.DoAfter;
using Content.Server.DoAfter;
using Content.Server.Humanoid;
using Content.Server.IdentityManagement;
using Content.Shared.Damage;
using Content.Shared.Actions;
using Content.Shared.Changeling;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Changeling.Devour;

public sealed class ChangelingDevourSystem : SharedChangelingDevourSystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoidSystem = default!;
    [Dependency] private readonly GrammarSystem _grammarSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaSystem = default!;
    [Dependency] private readonly IdentitySystem _identitySystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;

    public override void Initialize()
    {
        base.Initialize();

    }

    public override void StartSound(EntityUid uid, ChangelingDevourComponent component)
    {
        component.CurrentDevourSound = _audioSystem.PlayPvs(component.ConsumeTickNoise, uid)!.Value.Entity;
    }

    public override void StopSound(EntityUid uid, ChangelingDevourComponent component)
    {
        _audioSystem.Stop(component.CurrentDevourSound);
        component.CurrentDevourSound = null;
    }
}
