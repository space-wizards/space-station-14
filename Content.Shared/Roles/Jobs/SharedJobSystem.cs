using System.Diagnostics.CodeAnalysis;
using Content.Shared.Players;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;

namespace Content.Shared.Roles.Jobs;

/// <summary>
///     Handles the job data on mind entities.
/// </summary>
public abstract class SharedJobSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly SharedPlayerSystem _playerSystem = default!;

    public bool MindHasJobWithId(EntityUid? mindId, string prototypeId)
    {
        return CompOrNull<JobComponent>(mindId)?.PrototypeId == prototypeId;
    }

    public bool MindTryGetJob(
        [NotNullWhen(true)] EntityUid? mindId,
        [NotNullWhen(true)] out JobComponent? comp,
        [NotNullWhen(true)] out JobPrototype? prototype)
    {
        comp = null;
        prototype = null;

        return TryComp(mindId, out comp) &&
               comp.PrototypeId != null &&
               _prototypes.TryIndex(comp.PrototypeId, out prototype);
    }

    /// <summary>
    ///     Tries to get the job name for this mind.
    ///     Returns unknown if not found.
    /// </summary>
    public bool MindTryGetJobName([NotNullWhen(true)] EntityUid? mindId, out string name)
    {
        if (MindTryGetJob(mindId, out _, out var prototype))
        {
            name = prototype.LocalizedName;
            return true;
        }

        name = Loc.GetString("generic-unknown-title");
        return false;
    }

    /// <summary>
    ///     Tries to get the job name for this mind.
    ///     Returns unknown if not found.
    /// </summary>
    public string MindTryGetJobName([NotNullWhen(true)] EntityUid? mindId)
    {
        MindTryGetJobName(mindId, out var name);
        return name;
    }

    public bool CanBeAntag(ICommonSession player)
    {
        if (_playerSystem.ContentData(player) is not { Mind: { } mindId })
            return false;

        if (!MindTryGetJob(mindId, out _, out var prototype))
            return true;

        return prototype.CanBeAntag;
    }
}
