using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.Antag;

/// <summary>
/// Used for assigning specificied icons for antags.
/// </summary>
public abstract class AntagStatusIconSystem : SharedStatusIconSystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    public override void Initialize()
    {
        base.Initialize();
    }

    /// <summary>
    /// Can be called to give status icons to antags and any antag leaders.
    /// </summary>
    /// <param name="antagStatusIcon">The status icon that your antag uses</param>
    /// <param name="antagLeaderStatusIcon">The status icon of your antag leader (set to null if no leader)</param>
    /// <param name="args">The GetStatusIcon event.</param>
    protected virtual void GetStatusIcon(string antagStatusIcon, string? antagLeaderStatusIcon, ref GetStatusIconsEvent args)
    {
        if (antagLeaderStatusIcon == null)
        {
            args.StatusIcons.Add(_prototype.Index<StatusIconPrototype>(antagStatusIcon));
        }
        else
        {
            args.StatusIcons.Add(_prototype.Index<StatusIconPrototype>(antagLeaderStatusIcon));
        }
    }
}
