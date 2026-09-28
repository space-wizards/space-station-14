namespace Content.Server.Paper;

/// <summary>
/// Raised when a paper is copied from another. Allows other
/// systems to copy components from the original paper.
/// <param name="Copy">The entity of the new paper</param>
/// </summary>
public record struct PaperCopiedEvent(EntityUid Copy);
