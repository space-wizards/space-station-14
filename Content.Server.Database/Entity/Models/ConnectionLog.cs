namespace Content.Server.Database.Entity.Models
{
    using System;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Net;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    [Table("connection_log")]
    public class ConnectionLog : IEntityTypeConfiguration<ConnectionLog>
    {
        [Column("connection_log_id")]
        public int Id { get; set; }

        [Column("user_id")]
        public Guid UserId { get; set; }

        [Column("user_name")]
        public string UserName { get; set; } = null!;

        [Column("time")]
        public DateTime Time { get; set; }

        [Column("address")]
        public IPAddress Address { get; set; } = null!;

        public void Configure(EntityTypeBuilder<ConnectionLog> builder)
        {
            builder.HasIndex(p => p.UserId);
        }
    }
}