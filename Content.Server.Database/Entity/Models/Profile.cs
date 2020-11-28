namespace Content.Server.Database.Entity.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;
    using Content.Shared.Preferences;

    [Table("profile")]
    public class Profile : IEntityTypeConfiguration<Profile>
    {
        [Column("profile_id")]
        public int Id { get; set; }

        [Column("slot")]
        public int Slot { get; set; }

        [Column("char_name")]
        public string CharacterName { get; set; } = null!;

        [Column("age")]
        public int Age { get; set; }

        [Column("sex")]
        public string Sex { get; set; } = null!;

        [Column("hair_name")]
        public string HairName { get; set; } = null!;
        [Column("hair_color")]
        public string HairColor { get; set; } = null!;

        [Column("facial_hair_name")]
        public string FacialHairName { get; set; } = null!;
        [Column("facial_hair_color")]
        public string FacialHairColor { get; set; } = null!;

        [Column("eye_color")]
        public string EyeColor { get; set; } = null!;
        [Column("skin_color")]
        public string SkinColor { get; set; } = null!;

        public ICollection<Job> Jobs { get; set; } = null!;
        public List<Antag> Antags { get; set; } = null!;

        [Column("pref_unavailable")]
        public PreferenceUnavailableMode PreferenceUnavailable { get; set; }

        [Column("preference_id")]
        public int PreferenceId { get; set; }
        public Preference Preference { get; set; } = null!;

        public void Configure(EntityTypeBuilder<Profile> builder)
        {
            builder.HasIndex(p => new {p.Slot, PrefsId = p.PreferenceId})
                .IsUnique();
        }
    }
}