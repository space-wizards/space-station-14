using Content.Shared.CharacterAppearance;
using Content.Shared.Preferences;

namespace Content.Shared.Humanoid;

/// <summary>
///     HumanoidSystem. Primarily deals with the appearance and visual data
///     of a humanoid entity. HumanoidVisualizer is what deals with actually
///     organizing the sprites and setting up the sprite component's layers.
///
///     This is a shared system, because while it is server authoritative,
///     you still need a local copy so that players can set up their
///     characters.
/// </summary>
public sealed class SharedHumanoidSystem : EntitySystem
{
    public void Synchronize(EntityUid uid, AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref appearance))
        {
            return;
        }
    }

    // This one should set every single sprite up, like an initializer.
    //
    // (in fact, this could probably just be in the Visualizer)
    public void SetSprites(EntityUid uid, SharedHumanoidComponent? humanoid = null)
    {
    }

    public void LoadProfile(EntityUid uid, HumanoidCharacterProfile profile, SharedHumanoidComponent? humanoid = null)
    {
        if (!Resolve(uid, ref humanoid))
        {
            return;
        }
    }

    /// <summary>
    ///     Set an entire visual layer's visibility. This will affect all
    ///     markings and body parts. Server
    /// </summary>
    /// <param name="layer"></param>
    /// <param name="visible"></param>
    public void SetLayerVisibility(EntityUid uid, HumanoidVisualLayers layer, bool visible)
    {}

    /// <summary>
    ///     Sets a humanoid's skin color, including any sprite accessory that
    ///     follows skin color.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="color"></param>
    /// <param name="setAll">
    ///     If all visual layers on this humanoid should be set to this color.
    ///     If so, then it will override any existing marking color.
    /// </param>
    public void SetSkinColor(EntityUid uid, Color color, bool setAll = false)
    {}

    /// <summary>
    ///     Sets the color of a marking on this humanoid visual layer.
    /// </summary>
    /// <param name="layer"></param>
    /// <param name="markingId"></param>
    /// <param name="color"></param>
    public void SetMarkingColor(EntityUid uid, string markingId, Color color)
    {}

    /// <summary>
    ///     Adds a marking to this humanoid.
    /// </summary>
    /// <param name="layer"></param>
    /// <param name="markingId"></param>
    public void AddMarking(EntityUid uid, string markingId)
    {}

    /// <summary>
    ///     Removes a marking from this humanoid visual layer.
    /// </summary>
    /// <param name="layer"></param>
    /// <param name="markingId"></param>
    public void RemoveMarking(EntityUid uid, string markingId)
    {}
}
