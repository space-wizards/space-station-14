using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;
using Content.Shared.Ghost;
using Robust.Client.Player;

namespace Content.Client.Antag;

/// <summary>
/// Used for assigning specificied icons for antags.
/// </summary>
public abstract class AntagStatusIconSystem<T> : SharedStatusIconSystem
    where T : Component
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    public override void Initialize()
    {
        base.Initialize();
    }

    /// <summary>
    /// Will check if the local player has the same component as the one who called it and give the status icon.
    /// </summary>
    /// <param name="antagStatusIcon">The status icon that your antag uses</param>
    /// <param name="args">The GetStatusIcon event.</param>
    protected virtual void GetStatusIcon(string antagStatusIcon, ref GetStatusIconsEvent args)
    {
        if (!HasComp<T>(_player.LocalPlayer?.ControlledEntity))
        {
            if (HasComp<GhostComponent>(_player.LocalPlayer?.ControlledEntity))
            {

            }
            else return;
        }

        args.StatusIcons.Add(_prototype.Index<StatusIconPrototype>(antagStatusIcon));
    }
}
