namespace Content.Server.ParticleAccelerator.Wires;

public enum ParticleAcceleratorControlBoxWires
{
    /// <summary>
    /// Pulse toggles Power. Cut permanently turns off until Mend.
    /// </summary>
    Toggle,

    /// <summary>
    /// Pulsing increases level until at limit.
    /// </summary>
    Strength,

    /// <summary>
    /// Pulsing toggles Button-Disabled on UI. Cut disables, Mend enables.
    /// </summary>
    Interface,

    /// <summary>
    /// Pulsing will produce short message about whirring noise. Cutting increases the max level to 3. Mending reduces it back to 2.
    /// </summary>
    Limiter,

    /// <summary>
    /// Does Nothing
    /// </summary>
    Nothing
}
