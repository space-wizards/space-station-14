using Content.Shared.CCVar;

namespace Content.Server.Maps;

public sealed partial class GameMapSystem
{
    [ViewVariables] private bool _mapRotationEnabled;
    [ViewVariables] private int _mapRotationMemoryDepth;
    [ViewVariables] private bool _randomStationOffset;
    [ViewVariables] private bool _randomStationRotation;
    [ViewVariables] private float _maxRandomStationOffset;

    private void InitializeCVars()
    {
        _configurationManager.OnValueChanged(CCVars.GameMapRotation, value => _mapRotationEnabled = value, true);
        _configurationManager.OnValueChanged(CCVars.GameMap, value =>
        {
            if (value != string.Empty && _prototypeManager.HasIndex<GameMapPrototype>(value))
                _forcedMaps = new List<string>(1) { value };
            else if (value != string.Empty)
                throw new ArgumentException($"Unknown map prototype {value} was selected!");
        }, true);
        _configurationManager.OnValueChanged(CCVars.GameMapMemoryDepth, value => _mapRotationMemoryDepth = value, true);
        _configurationManager.OnValueChanged(CCVars.StationOffset, x => _randomStationOffset = x, true);
        _configurationManager.OnValueChanged(CCVars.MaxStationOffset, x => _maxRandomStationOffset = x, true);
        _configurationManager.OnValueChanged(CCVars.StationRotation, x => _randomStationRotation = x, true);
    }
}
