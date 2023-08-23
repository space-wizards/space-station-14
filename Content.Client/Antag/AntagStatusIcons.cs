using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Client.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.Antag;

public abstract class AntagStatusIcons<T, TB> : EntitySystem
    where T : Component
    where TB : Component
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    public override void Initialize()
    {
        base.Initialize();
    }

    protected virtual void GetStatusIcon(EntityUid uid, T antag, TB? leader, string antagStatusIcon, string? antagLeaderStatusIcon, ref GetStatusIconsEvent args)
    {
        if (!HasComp<T>(_player.LocalPlayer?.ControlledEntity))
            return;

        if (antagLeaderStatusIcon == null)
        {
            args.StatusIcons.Add(_prototype.Index<StatusIconPrototype>(antagStatusIcon));
        }
        else
        {
            if (HasComp<T>(uid) && !HasComp<TB>(uid))
            {
                args.StatusIcons.Add(_prototype.Index<StatusIconPrototype>(antagStatusIcon));
            }
            else
            {
                args.StatusIcons.Add(_prototype.Index<StatusIconPrototype>(antagLeaderStatusIcon));
            }
        }
    }
}
