namespace Content.Server.Database.Entity.Models
{
    using System.ComponentModel.DataAnnotations.Schema;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;
    using Content.Shared.Preferences;

    [Table("job")]
    public class Job : IEntityTypeConfiguration<Job>
    {
        [Column("job_id")]
        public int Id { get; set; }

        public Profile Profile { get; set; } = null!;
        [Column("profile_id")]
        public int ProfileId { get; set; }

        [Column("job_name")]
        public string JobName { get; set; } = null!;
        [Column("priority")]
        public JobPriority Priority { get; set; }

        public void Configure(EntityTypeBuilder<Job> builder)
        {
        }
    }
}