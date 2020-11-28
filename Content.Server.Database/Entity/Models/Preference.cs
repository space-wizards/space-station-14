namespace Content.Server.Database.Entity.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations.Schema;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    [Table("preference")]
    public class Preference : IEntityTypeConfiguration<Preference>
    {
        // NOTE: on postgres there SHOULD be an FK ensuring that the selected character slot always exists.
        // I had to use a migration to implement it and as a result its creation is a finicky mess.
        // Because if I let EFCore know about it it would explode on a circular reference.
        // Also it has to be DEFERRABLE INITIALLY DEFERRED so that insertion of new preferences works.
        // Also I couldn't figure out how to create it on SQLite.

        [Column("preference_id")]
        public int Id { get; set; }

        [Column("user_id")]
        public Guid UserId { get; set; }

        [Column("selected_character_slot")]
        public int SelectedCharacterSlot { get; set; }

        public ICollection<Profile> Profiles { get; set; } = null!;

        public void Configure(EntityTypeBuilder<Preference> builder)
        {
            builder.HasIndex(p => p.UserId)
                .IsUnique();
        }
    }
}