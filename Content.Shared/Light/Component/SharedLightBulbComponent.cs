using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared.Light.Component;

/// <summary>
/// Component that represents a light bulb. Can be broken, or burned, which turns them mostly useless.
/// TODO: Breaking and burning should probably be moved to another component eventually.
/// </summary>
[RegisterComponent]
public sealed class LightBulbComponent : Robust.Shared.GameObjects.Component
{
    /// <summary>
    /// The color of the lightbulb and the light it produces.
    /// </summary>
    [DataField("color")]
    public Color Color = Color.White;

    /// <summary>
    /// The type of lightbulb. Tube/bulb/etc...
    /// </summary>
    [DataField("bulb")]
    public LightBulbType Type = LightBulbType.Tube;

    /// <summary>
    /// The initial state of the lightbulb.
    /// </summary>
    [DataField("startingState")]
    public LightBulbState State = LightBulbState.Normal;

    /// <summary>
    /// The temperature the air around the lightbulb is exposed to when the lightbulb burns out.
    /// </summary>
    [DataField("BurningTemperature")]
    public int BurningTemperature = 1400;

    /// <summary>
    /// Relates to how bright the light produced by the lightbulb is.
    /// </summary>
    [DataField("lightEnergy")]
    public float LightEnergy = 0.8f;

    /// <summary>
    /// The maximum radius of the point light source this light produces.
    /// </summary>
    [DataField("lightRadius")]
    public float LightRadius = 10;

    /// <summary>
    /// Relates to the falloff constant of the light produced by the lightbulb.
    /// </summary>
    [DataField("lightSoftness")]
    public float LightSoftness = 1;

    /// <summary>
    /// The amount of power used by the lightbulb when it's active.
    /// </summary>
    [DataField("PowerUse")]
    public int PowerUse = 60;
    
    /// <summary>
    /// The sound produced when the lightbulb breaks.
    /// </summary>
    [DataField("breakSound")]
    public SoundSpecifier BreakSound = new SoundCollectionSpecifier("GlassBreak");

    #region Appearance

    /// <summary>
    /// The sprite state used when the lightbulb is intact.
    /// </summary>
    [DataField("normalSpriteState")]
    public string NormalSpriteState = "normal";

    /// <summary>
    /// The sprite state used when the lightbulb is broken.
    /// </summary>
    [DataField("brokenSpriteState")]
    public string BrokenSpriteState = "broken";

    /// <summary>
    /// The sprite state used when the lightbulb is burned.
    /// </summary>
    [DataField("burnedSpriteState")]
    public string BurnedSpriteState = "burned";

    #endregion Appearance
}

[Serializable, NetSerializable]
public enum LightBulbState : byte
{
    Normal,
    Broken,
    Burned,
}

[Serializable, NetSerializable]
public enum LightBulbVisuals : byte
{
    State,
    Color
}

[Serializable, NetSerializable]
public enum LightBulbType : byte
{
    Bulb,
    Tube,
}

[Serializable, NetSerializable]
public enum LightBulbVisualLayers : byte
{
    Base,
}
