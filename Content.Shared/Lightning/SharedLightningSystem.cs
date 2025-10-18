using Robust.Shared.Random;

namespace Content.Shared.Lightning;

public abstract class SharedLightningSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    /// <summary>
    /// Picks a random sprite state for the lightning. It's just data that gets passed to the <see cref="BeamComponent"/>
    /// </summary>
    /// <returns>Returns a string "lightning_" + the chosen random number.</returns>
    public string LightningRandomizer()
    {
        //When the lightning is made with TryCreateBeam, spawns random sprites for each beam to make it look nicer.
        var spriteStateNumber = _random.Next(1, 12);
        return ("lightning_" + spriteStateNumber);
    }
}
