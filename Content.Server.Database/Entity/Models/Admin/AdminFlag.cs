
namespace Content.Server.Database.Entity.Models
{
    using System;
    using System.ComponentModel.DataAnnotations.Schema;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    [Table("admin_flag")]
    public class AdminFlag : IEntityTypeConfiguration<AdminFlag>
    {
        [Column("admin_flag_id")]
        public int Id { get; set; }

        [Column("flag")]
        public string Flag { get; set; } = null!;

        [Column("negative")]
        public bool Negative { get; set; }

        [Column("admin_id")]
        public Guid AdminId { get; set; }
        public Admin Admin { get; set; } = null!;

        public void Configure(EntityTypeBuilder<AdminFlag> builder)
        {
            builder.HasIndex(f => new {f.Flag, f.AdminId})
                .IsUnique();
        }
    }
}