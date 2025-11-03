namespace Content.Server.Atmos;

/// <summary>
/// Internal Atmospherics struct that stores data about a hotspot in a tile.
/// Hotspots are used to model flammable gas ignition and fires.
/// </summary>
public struct Hotspot
{
    /// <summary>
    /// Whether this hotspot is currently representing fire.
    /// Set when the hotspot "becomes alight". This is never set to false
    /// because Atmospherics will just assign <see cref="TileAtmosphere"/>
    /// a new <see cref="Hotspot"/> struct when the fire goes out.
    /// </summary>
    [ViewVariables]
    public bool Valid;

    /// <summary>
    /// Whether this hotspot has skipped its first process cycle.
    /// AtmosphereSystem.Hotspot skips processing a hotspot beyond
    /// setting it to active (for LINDA processing) the first
    /// time it is processed.
    /// </summary>
    [ViewVariables]
    public bool SkippedFirstProcess;

    /// <summary>
    /// <para>Whether this hotspot is currently using the tile for reacting and fire processing
    /// instead of a fraction of the tile's air.</para>
    ///
    /// <para>When a tile is considered a hotspot, Hotspot will pull a fraction of that tile's
    /// air out of the tile and perform a reaction on that air, merging it back afterward.
    /// Bypassing triggers when the hotspot volume nears the tile's volume, making the system
    /// use the tile's GasMixture instead of pulling a fraction out.</para>
    /// </summary>
    [ViewVariables]
    public bool Bypassing;

    [ViewVariables]
    public float Temperature;

    [ViewVariables]
    public float Volume;

    /// <summary>
    ///     State for the fire sprite.
    /// </summary>
    [ViewVariables]
    public byte State;
}
