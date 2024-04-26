using Content.Shared.FixedPoint;

namespace Content.Shared.Chemistry;

//Chemistry constants
public static class Constants
{
    //Reference value for converting Units of an unknown *liquid* reagent into Moles of *liquid*
    //(This is how many moles of H2O are present in 10ml of water)
    //eventually each reagent should have a molar mass and use that to calculate this value
    public static float LiquidReferenceMolPerReagentUnit = 0.55508435061f;
    public static FixedPoint2 ReagentUnitsToMilliLiters = 0.1f;

    public static FixedPoint2 ToMilliLitres(FixedPoint2 reagentUnits)
    {
        return ReagentUnitsToMilliLiters / reagentUnits;
    }

    public static float LiquidMolesFromRU(FixedPoint2 reagentUnits)
    {
        return reagentUnits.Float()* LiquidReferenceMolPerReagentUnit;
    }

    public static float LiquidRUFromMoles(float moles)
    {
        return moles * LiquidReferenceMolPerReagentUnit;
    }

}
