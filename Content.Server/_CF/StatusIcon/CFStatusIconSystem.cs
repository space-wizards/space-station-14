using Content.Shared.StatusIcon;

namespace Content.Server.CF.StatusIcon;

/// <summary>
/// Server half of <see cref="SharedStatusIconSystem"/> so flanking can attach the icon anchor.
/// The icons themselves are drawn on the client.
/// </summary>
public sealed class CFStatusIconSystem : SharedStatusIconSystem;
