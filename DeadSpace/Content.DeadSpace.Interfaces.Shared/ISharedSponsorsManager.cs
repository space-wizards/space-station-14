using System.Diagnostics.CodeAnalysis;

namespace Content.DeadSpace.Interfaces.Shared;

public interface ISharedSponsorsManager
{
    void Initialize();
}

public interface ISponsorInfo
{
    string CharacterName { get; set; }
    int? Tier { get; set; }
    string? OOCColor { get; set; }
    bool HavePriorityJoin { get; set; }
    int ExtraSlots { get; set; }
    string[] AllowedMarkings { get; set; }
    DateTime ExpireDate { get; set; }
    bool AllowJob { get; set; }
    bool HavePriorityAntag { get; set; }
}
