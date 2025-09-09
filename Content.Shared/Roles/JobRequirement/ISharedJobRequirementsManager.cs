using System.Diagnostics.CodeAnalysis;
using Content.Shared.Preferences;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Roles;

public interface ISharedJobRequirementsManager
{
    public bool IsAllowed(ICommonSession session,
        ProtoId<JobPrototype> job,
        HumanoidCharacterProfile? profile,
        [NotNullWhen(false)] out FormattedMessage? reason);
}
