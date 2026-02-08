using System.Diagnostics.CodeAnalysis;
using Content.Shared.CCVar;

namespace Content.Shared.Atmos.EntitySystems;

public abstract partial class SharedAtmosphereSystem
{
    /*
     Partial class for storing shared configuration values.
     */

    public float HeatScale { get; private set; }

    [SuppressMessage("ReSharper", "BadExpressionBracesLineBreaks")]
    [SuppressMessage("ReSharper", "MultipleStatementsOnOneLine")]
    private void InitializeCVars()
    {
        Subs.CVar(_cfg, CCVars.AtmosHeatScale, value => { HeatScale = value; InitializeGases(); }, true);
    }
}
