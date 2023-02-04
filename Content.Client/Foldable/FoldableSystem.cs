using Content.Shared.Foldable;
using JetBrains.Annotations;

namespace Content.Client.Foldable;

[UsedImplicitly]
public sealed class FoldableSystem : SharedFoldableSystem
{
    // classic.
}

/// <summary>
/// The sprite layers that should be modified if something is folded/unfolded.
/// <example>
/// For example
/// <code>
/// - type: GenericVisualizer
///   visuals:
///     enum.FoldedVisuals.State:
///       enum.FoldableVisualLayers.Base:
///         True: {state: "rollerbed_folded"}
///         False: {state: "rollerbed"}
/// </code>
/// makes rollerbeds visually respond to being folded.
/// As the base sprite state, in this case 'rollerbed', is usually unique for each entity this snippit will need to be copypasta'd for each.
/// </example>
/// </summary>
public enum FoldableVisualLayers : byte
{
    Base
}
