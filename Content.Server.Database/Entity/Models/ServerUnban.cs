namespace Content.Server.Database.Entity.Models
{
    using System;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Net;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    [Table("server_unban")]
    public class ServerUnban : IEntityTypeConfiguration<ServerUnban>
    {
        [Column("unban_id")]
        public int Id { get; set; }

        [Column("ban_id")]
        public int BanId { get; set; }
        public ServerBan Ban { get; set; } = null!;

        [Column("unbanning_admin")]
        public Guid? UnbanningAdmin { get; set; }

        [Column("unban_time")]
        public DateTimeOffset UnbanTime { get; set; }

        public void Configure(EntityTypeBuilder<ServerUnban> builder)
        {
            builder.HasIndex(p => p.BanId)
                .IsUnique();
        }
    }
}