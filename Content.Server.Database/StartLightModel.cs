using System.Collections.Generic;

namespace Content.Server.Database;

public sealed class StarLightModel
{
    public class StarLightProfile
    {
        public int Id { get; set; }
        public int ProfileId { get; set; }
        public virtual Profile Profile { get; set; } = null!;
        public string? CustomSpecieName { get; set; }
        public List<string> CyberneticIds { get; set; } = [];
        public float Width { get; set; } = 1f;
        public float Height { get; set; } = 1f;
    }
}
