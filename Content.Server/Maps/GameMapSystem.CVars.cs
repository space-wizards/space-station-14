using Content.Shared.CCVar;

namespace Content.Server.Maps;

public sealed partial class GameMapSystem
{
    [ViewVariables] private bool _mapRotationEnabled;

    private void InitializeCVars()
    {
        _configurationManager.OnValueChanged(CCVars.GameMapRotation, value => _mapRotationEnabled = value, true);
        _configurationManager.OnValueChanged(CCVars.GameMap, value =>
        {
            if (value != string.Empty && _prototypeManager.HasIndex<GameMapPrototype>(value))
                _forcedMaps = new List<string>(1) {value};
            else if (value != string.Empty)
                throw new ArgumentException($"Unknown map prototype {value} was selected!");
        }, true);
    }
}
