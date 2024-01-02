using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared.Traits.Assorted;

public abstract class SharedIlliterateSystem : EntitySystem
{
}

/// <summary>
/// Raised when there is an attempt to read an entity such as paper
/// </summary>
public sealed class ReadAttemptEvent : EventArgs
{
    /// <summary>
    /// Should the reader be able understand what is written
    /// </summary>
    public bool CanRead;

    /// <summary>
    /// Who is attmpting to read
    /// </summary>
    public EntityUid? Reader;

    /// <summary>
    /// What are they attempting to read
    /// </summary>
    public EntityUid? EntityRead;

    public ReadAttemptEvent(EntityUid? reader, EntityUid? entityRead)
    {
        Reader = reader;
        EntityRead = entityRead;
        CanRead = true;
    }
}

/// <summary>
/// Raised when there is an attempt to write to an entity such as paper
/// </summary>
public sealed class WriteAttemptEvent : EventArgs
{
    /// <summary>
    /// Can the writer actually write
    /// </summary>
    public bool CanWrite;

    /// <summary>
    /// Who is attmpting to write
    /// </summary>
    public EntityUid? Writer;

    /// <summary>
    /// What are they attempting to write on
    /// </summary>
    public EntityUid? EntityWritten;

    public WriteAttemptEvent(EntityUid? writer, EntityUid? entityWritten)
    {
        Writer = writer;
        EntityWritten = entityWritten;
        CanWrite = true;
    }
}
