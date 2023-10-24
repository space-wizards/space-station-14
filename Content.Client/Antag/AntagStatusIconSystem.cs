using Content.Shared.CCVar;
using Content.Shared.Ghost;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Client.Antag;

/// <summary>
/// Used for assigning specified icons for antags.
/// </summary>
public abstract class AntagStatusIconSystem<T> : SharedStatusIconSystem
    where T : IComponent
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private bool GhostAntagStatusIndicatorVisible {get; set;}

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(CCVars.GhostAntagStatusIndicatorVisible, value => GhostAntagStatusIndicatorVisible =  value, true);

    }

    /// <summary>
    /// Will check if the local player has the same component as the one who called it and give the status icon.
    /// </summary>
    /// <param name="antagStatusIcon">The status icon that your antag uses</param>
    /// <param name="args">The GetStatusIcon event.</param>
    protected virtual void GetStatusIcon(string antagStatusIcon, ref GetStatusIconsEvent args)
    {
        var ent = _player.LocalPlayer?.ControlledEntity;

        if (!HasComp<T>(ent))
        {
            // We still want admin ghosts to see the icons even if normal ghosts can't.
            if (!TryComp<GhostComponent>(ent, out var comp) || (!GhostAntagStatusIndicatorVisible && !comp.CanGhostInteract))
                return;
        }

        args.StatusIcons.Add(_prototype.Index<StatusIconPrototype>(antagStatusIcon));
    }
}
