namespace Content.Server.Database.Entity.Models
{
    using System.ComponentModel.DataAnnotations.Schema;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    [Table("admin_rank_flag")]
    public class AdminRankFlag : IEntityTypeConfiguration<AdminRankFlag>
    {
        [Column("admin_rank_flag_id")]
        public int Id { get; set; }
        [Column("flag")]
        public string Flag { get; set; } = null!;

        [Column("admin_rank_id")]
        public int AdminRankId { get; set; }
        public AdminRank Rank { get; set; } = null!;

        public void Configure(EntityTypeBuilder<AdminRankFlag> builder)
        {
            builder.HasIndex(f => new {f.Flag, f.AdminRankId})
                .IsUnique();
        }
    }
}