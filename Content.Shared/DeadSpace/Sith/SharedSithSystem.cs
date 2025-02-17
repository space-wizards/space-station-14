// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Actions;
using Robust.Shared.Serialization;
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Content.Shared.DeadSpace.Sith.Components;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Shared.DeadSpace.Sith;

public abstract class SharedSithSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SithComponent, ShotAttemptedEvent>(OnShotAttempted);
    }

    private void OnShotAttempted(Entity<SithComponent> ent, ref ShotAttemptedEvent args)
    {
        _popup.PopupClient(Loc.GetString("gun-disabled"), ent, ent);
        args.Cancel();
    }
}

public sealed partial class SithSubmissionEvent : EntityTargetActionEvent { }

public sealed partial class SithLightningEvent : EntityTargetActionEvent { }

public sealed partial class SithForceOneEvent : EntityTargetActionEvent { }

public sealed partial class SithForceEvent : InstantActionEvent { }

public sealed partial class SithShieldEvent : InstantActionEvent { }

[Serializable, NetSerializable]
public sealed partial class SithSubmissionDoAfterEvent : SimpleDoAfterEvent { }
