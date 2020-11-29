namespace Content.Server.Database.Entity.Models
{
    using System;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Net;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    [Table("server_ban")]
    public class ServerBan : IEntityTypeConfiguration<ServerBan>
    {
        [Column("server_ban_id")]
        public int Id { get; set; }

        [Column("user_id")]
        public Guid? UserId { get; set; } // NetUserId is just Guid

        [Column("address")]
        public (IPAddress, int)? Address { get; set; }

        [Column("ban_time")]
        public DateTime BanTime { get; set; }

        [Column("expiration_time")]
        public DateTime? ExpirationTime { get; set; }

        [Column("reason")]
        public string Reason { get; set; } = null!;

        [Column("banning_admin")]
        public Guid? BanningAdmin { get; set; }

        public ServerUnban? Unban { get; set; }

        public void Configure(EntityTypeBuilder<ServerBan> builder)
        {
            builder.HasIndex(p => p.UserId);
            builder.HasIndex(p => p.Address);
            builder.HasIndex(p => p.UserId);

            builder.HasCheckConstraint(
                "HaveEitherAddressOrUserId",
                "address IS NOT NULL OR user_id IS NOT NULL"
            );
        }
    }
}
