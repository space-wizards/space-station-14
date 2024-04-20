using Content.Shared.FixedPoint;

namespace Content.Shared.Chemistry;

//Chemistry constants
public static class Constants
{
    //Reference value for converting Units of an unknown reagent into Moles
    //(This is how many moles of H2O are present in 10ml of water)
    //eventually each reagent should have its own conversion value
    public static float ReferenceMolPerReagentUnit = 0.55508435061f;
    public static FixedPoint2 ReagentUnitsToMilliLiters = 0.1f;

    public static FixedPoint2 ToMl(FixedPoint2 reagentUnits)
    {
        return ReagentUnitsToMilliLiters * reagentUnits;
    }

    public static float MolesFromRU(FixedPoint2 reagentUnits)
    {
        return reagentUnits.Float()* ReferenceMolPerReagentUnit;
    }

    public static FixedPoint2 RUFromMoles(float moles)
    {
        return moles/ReferenceMolPerReagentUnit;
    }

}
