using Content.Shared.CCVar;

namespace Content.Server.Radiation.Systems;

// cvar updates
public partial class RadiationSystem
{
    public float MinIntensity { get; private set; }
    public float GridcastUpdateRate { get; private set; }
    public bool GridcastSimplifiedSameGrid { get; private set; }
    public float GridcastMaxDistance { get; private set; }

    private void SubscribeCvars()
    {
        _cfg.OnValueChanged(CCVars.RadiationMinIntensity, SetMinRadiationIntensity, true);
        _cfg.OnValueChanged(CCVars.RadiationGridcastUpdateRate, SetGridcastUpdateRate, true);
        _cfg.OnValueChanged(CCVars.RadiationGridcastSimplifiedSameGrid, SetGridcastSimplifiedSameGrid, true);
        _cfg.OnValueChanged(CCVars.RadiationGridcastMaxDistance, SetGridcastMaxDistance, true);
    }

    private void UnsubscribeCvars()
    {
        _cfg.UnsubValueChanged(CCVars.RadiationMinIntensity, SetMinRadiationIntensity);
        _cfg.UnsubValueChanged(CCVars.RadiationGridcastUpdateRate, SetGridcastUpdateRate);
        _cfg.UnsubValueChanged(CCVars.RadiationGridcastSimplifiedSameGrid, SetGridcastSimplifiedSameGrid);
        _cfg.UnsubValueChanged(CCVars.RadiationGridcastMaxDistance, SetGridcastMaxDistance);
    }

    private void SetMinRadiationIntensity(float radiationMinIntensity)
    {
        MinIntensity = radiationMinIntensity;
    }

    private void SetGridcastUpdateRate(float updateRate)
    {
        GridcastUpdateRate = updateRate;
    }

    private void SetGridcastSimplifiedSameGrid(bool simplifiedSameGrid)
    {
        GridcastSimplifiedSameGrid = simplifiedSameGrid;
    }

    private void SetGridcastMaxDistance(float maxDistance)
    {
        GridcastMaxDistance = maxDistance;
    }
}
