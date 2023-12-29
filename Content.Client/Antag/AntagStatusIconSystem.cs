using Content.Shared.Overlays;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Content.Shared.Revolutionary.Components;
using Robust.Client.Player;
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
    //Need to be changed for directly get the string from the dedicated component
    public ProtoId<StatusIconPrototype> RevStatusIcon = "RevolutionaryFaction";
    public ProtoId<StatusIconPrototype> HeadRevStatusIcon = "HeadRevolutionaryFaction";


    /// <summary>
    /// Will check if the local player has the same component as the one who called it and give the status icon.
    /// </summary>
    /// <param name="antagStatusIcon">The status icon that your antag uses</param>
    /// <param name="args">The GetStatusIcon event.</param>
    protected virtual void GetStatusIcon(string antagStatusIcon, ref GetStatusIconsEvent args, ShowAntagIconsComponent? showAntag = default!)
    {
        var ent = _player.LocalPlayer?.ControlledEntity;

        if (!HasComp<T>(ent) && !HasComp<ShowAntagIconsComponent>(ent))
            return;

        //If has the ShowAntagIconsComponent and Hideheadrev=true, replace the headrev icon by simple rev icon
        if (antagStatusIcon == HeadRevStatusIcon && TryComp<ShowAntagIconsComponent>(ent, out showAntag) && showAntag.Hideheadrev == true)
        {
            args.StatusIcons.Add(_prototype.Index<StatusIconPrototype>(RevStatusIcon));
            return;
        }

        args.StatusIcons.Add(_prototype.Index<StatusIconPrototype>(antagStatusIcon));
    }
}
