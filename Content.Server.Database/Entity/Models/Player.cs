namespace Content.Server.Database.Entity.Models
{
    using System;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Net;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    [Table("player")]
    public class Player : IEntityTypeConfiguration<Player>
    {
        [Column("player_id")]
        public int Id { get; set; }

        [Column("user_id")]
        public Guid UserId { get; set; }

        [Column("first_seen_time")]
        public DateTime FirstSeenTime { get; set; }

        [Column("last_seen_user_name")]
        public string LastSeenUserName { get; set; } = null!;

        [Column("last_seen_time")]
        public DateTime LastSeenTime { get; set; }

        [Column("last_seen_address")]
        public IPAddress LastSeenAddress { get; set; } = null!;

        public void Configure(EntityTypeBuilder<Player> builder)
        {
            builder.HasIndex(p => p.UserId)
                .IsUnique();

            builder.HasIndex(p => p.LastSeenUserName);
        }
    }
}