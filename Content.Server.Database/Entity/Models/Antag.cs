namespace Content.Server.Database.Entity.Models
{
    using System.ComponentModel.DataAnnotations.Schema;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    [Table("antag")]
    public class Antag : IEntityTypeConfiguration<Antag>
    {
        [Column("antag_id")] public int Id { get; set; }
        public Profile Profile { get; set; } = null!;
        [Column("profile_id")] public int ProfileId { get; set; }

        [Column("antag_name")] public string AntagName { get; set; } = null!;

        public void Configure(EntityTypeBuilder<Antag> builder)
        {
            builder.HasIndex(p => new {HumanoidProfileId = p.ProfileId, p.AntagName})
                .IsUnique();
        }
    }
}