using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    public static readonly CVarDef<bool> SolidFuelEnabled =
        CVarDef.Create("fire.solid_fuel_enabled", true, CVar.SERVERONLY);
    public static readonly CVarDef<float> SolidFuelIgnitionMultiplier =
        CVarDef.Create("fire.solid_fuel_ignition_multiplier", 1f, CVar.SERVERONLY);
    public static readonly CVarDef<float> SolidFuelBurnMultiplier =
        CVarDef.Create("fire.solid_fuel_burn_multiplier", 1f, CVar.SERVERONLY);
    public static readonly CVarDef<bool> SolidFuelSpread =
        CVarDef.Create("fire.solid_fuel_spread", true, CVar.SERVERONLY);
    public static readonly CVarDef<float> SolidFuelContactRange =
        CVarDef.Create("fire.solid_fuel_contact_range", 0.6f, CVar.SERVERONLY);
    public static readonly CVarDef<float> SolidFuelSpreadRange =
        CVarDef.Create("fire.solid_fuel_spread_range", 1.1f, CVar.SERVERONLY);
    public static readonly CVarDef<float> SolidFuelFireRate =
        CVarDef.Create("fire.solid_fuel_fire_rate", 20f, CVar.SERVERONLY);
    public static readonly CVarDef<float> SolidFuelCigaretteRate =
        CVarDef.Create("fire.solid_fuel_cigarette_rate", 1f, CVar.SERVERONLY);
}
