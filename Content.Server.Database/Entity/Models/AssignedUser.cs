namespace Content.Server.Database.Entity.Models
{
    using System;
    using System.ComponentModel.DataAnnotations.Schema;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    [Table("assigned_user_id")]
    public class AssignedUser : IEntityTypeConfiguration<AssignedUser>
    {
        [Column("assigned_user_id_id")] public int Id { get; set; }
        [Column("user_name")] public string UserName { get; set; } = null!;

        [Column("user_id")] public Guid UserId { get; set; }

        public void Configure(EntityTypeBuilder<AssignedUser> builder)
        {
            // Can't have two usernames with the same user ID.
            builder.HasIndex(p => p.UserId)
                .IsUnique();
            builder.HasIndex(p => p.UserName)
                .IsUnique();
        }
    }
}
