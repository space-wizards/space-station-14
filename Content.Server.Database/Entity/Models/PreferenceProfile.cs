namespace Content.Server.Database.Entity.Models
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    public class PreferenceProfile : IEntityTypeConfiguration<PreferenceProfile>
    {
        public int PreferenceId { get; set; }
        public Preference Preference { get; set; } = null!;

        public int ProfileId { get; set; }

        public Profile Profile { get; set; } = null!;

        public void Configure(EntityTypeBuilder<PreferenceProfile> builder)
        {
            builder.HasKey(p => new { p.PreferenceId, p.ProfileId });
            builder.HasIndex(p => p.PreferenceId)
                .IsUnique();
        }
    }
}