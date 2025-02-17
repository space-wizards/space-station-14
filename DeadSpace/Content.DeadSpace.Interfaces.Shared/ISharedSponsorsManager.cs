using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Content.DeadSpace.Interfaces.Shared;

public interface ISharedSponsorsManager
{
    public void Initialize();
}

public interface ISponsorInfo
{
    public string CharacterName { get; set; }
    public int? Tier { get; set; }
    public string? OOCColor { get; set; }
    public bool HavePriorityJoin { get; set; }
    public int ExtraSlots { get; set; }
    public string[] AllowedMarkings { get; set; }
    public DateTime ExpireDate { get; set; }
    public bool AllowJob { get; set; }
    public bool HavePriorityAntag { get; set; }
}
