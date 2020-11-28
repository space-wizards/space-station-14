namespace Content.Server.Database.Entity.Models
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations.Schema;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    [Table("admin_rank")]
    public class AdminRank : IEntityTypeConfiguration<AdminRank>
    {
        [Column("admin_rank_id")]
        public int Id { get; set; }
        [Column("name")]
        public string Name { get; set; } = null!;

        public ICollection<Admin> Admins { get; set; } = null!;
        public ICollection<AdminRankFlag> Flags { get; set; } = null!;

        public void Configure(EntityTypeBuilder<AdminRank> builder) {

        }
    }
}