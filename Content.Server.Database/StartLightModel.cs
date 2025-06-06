namespace Content.Server.Database;

public sealed class StarLightModel
{
    public class StarLightProfile
    {
        [Column("starlightprofile_id")]
        public int Id { get; set; }
        public int ProfileId { get; set; }
        public virtual Profile Profile { get; set; } = null!;
        public string? CustomSpeciesName { get; set; }
    }
}