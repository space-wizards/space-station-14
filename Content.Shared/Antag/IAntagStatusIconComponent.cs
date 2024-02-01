using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;

namespace Content.Shared.Antag;

public interface IAntagStatusIconComponent
{
    public ProtoId<StatusIconPrototype> StatusIcon { get; set; }

    public bool IconVisibleToGhost { get; set; }
}

