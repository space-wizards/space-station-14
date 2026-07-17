using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database;

//
// Contains model definitions primarily related to custom vote logging.
//

internal static class ModelCustomVoteLog
{
    public static void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CustomVoteLog>()
            .HasOne(cvl => cvl.Initiator)
            .WithMany()
            .HasForeignKey(cvl => cvl.InitiatorId)
            .HasPrincipalKey(p => p.UserId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        modelBuilder.Entity<CustomVoteLogOption>()
            .HasKey(cvl => new { cvl.VoteId, cvl.OptionIdx });
    }
}

/// <summary>
/// A single admin-initiated custom vote logged in the database.
/// </summary>
public sealed class CustomVoteLog
{
    public int Id { get; set; }

    /// <summary>
    /// The round ID the vote was made in.
    /// </summary>
    public int RoundId { get; set; }

    /// <summary>
    /// The time the vote was created at.
    /// </summary>
    public DateTime TimeCreated { get; set; }

    /// <summary>
    /// The player-facing title for the vote.
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// The user ID of the admin that created the vote.
    /// </summary>
    public Guid? InitiatorId { get; set; }

    /// <summary>
    /// State of the vote.
    /// </summary>
    public CustomVoteState State { get; set; }

    /// <summary>
    /// The playing-facing options the vote has.
    /// </summary>
    public List<CustomVoteLogOption>? Options { get; set; }

    /// <summary>
    /// The round the vote was made in.
    /// </summary>
    public Round? Round { get; set; }

    /// <summary>
    /// The admin that created the vote.
    /// </summary>
    public Player? Initiator { get; set; }
}

/// <summary>
/// An option for a logged <see cref="CustomVoteLog"/>.
/// </summary>
public sealed class CustomVoteLogOption
{
    /// <summary>
    /// The vote ID this option is part of.
    /// </summary>
    public int VoteId { get; set; }

    /// <summary>
    /// The index of the option in the option list.
    /// </summary>
    public short OptionIdx { get; set; }

    /// <summary>
    /// The player-facing text for this option.
    /// </summary>
    public required string Text { get; set; }

    /// <summary>
    /// How many players voted for this option.
    /// </summary>
    /// <remarks>
    /// This field is only populated for votes with state <see cref="CustomVoteState.Finished"/>.
    /// </remarks>
    public int VoteCount { get; set; }

    /// <summary>
    /// The vote this option is part of.
    /// </summary>
    public CustomVoteLog? Vote { get; set; }
}

/// <summary>
/// Possible states for <see cref="CustomVoteLog"/> entries.
/// </summary>
public enum CustomVoteState : byte
{
    /// <summary>
    /// The vote is still active (likely was just made).
    /// </summary>
    Active,

    /// <summary>
    /// The vote finished and vote counts were written to database.
    /// </summary>
    Finished,

    /// <summary>
    /// The vote was canceled, no vote counts have been written to database.
    /// </summary>
    Cancelled,
}
