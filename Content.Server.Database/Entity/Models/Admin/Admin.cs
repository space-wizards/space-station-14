namespace Content.Server.Database.Entity.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    [Table("admin")]
    public class Admin : IEntityTypeConfiguration<Admin>
    {
        [Column("user_id"), Key] public Guid UserId { get; set; }
        [Column("title")] public string? Title { get; set; }

        [Column("admin_rank_id")] public int? AdminRankId { get; set; }
        public AdminRank? AdminRank { get; set; }
        public ICollection<AdminFlag> Flags { get; set; } = null!;

        public void Configure(EntityTypeBuilder<Admin> builder)
        {
            builder.HasOne(p => p.AdminRank)
                .WithMany(p => p!.Admins)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
