using Robust.Shared.Configuration;

namespace Content.Shared.Starlight.CCVar;

public sealed partial class StarlightCCVars
{
    // Taken from https://github.com/RMC-14/RMC-14
    public static readonly CVarDef<bool> PhysicsActiveInputMoverEnabled =
        CVarDef.Create("physics.active_input_mover_enabled", true, CVar.REPLICATED | CVar.SERVER);
}