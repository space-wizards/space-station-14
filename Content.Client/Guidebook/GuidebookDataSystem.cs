using System.Diagnostics.CodeAnalysis;
using Content.Shared.Guidebook;

namespace Content.Client.Guidebook;

public sealed class GuidebookDataSystem : EntitySystem
{
    private GuidebookData? _data;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<UpdateGuidebookDataEvent>(OnServerUpdated);

        RaiseNetworkEvent(new RequestGuidebookDataEvent());
    }

    private void OnServerUpdated(UpdateGuidebookDataEvent args)
    {
        Log.Debug("Got Server Prototype data");
        foreach (var (prototype, components) in args.Data.Data)
        {
            Log.Debug($"{prototype}");
            foreach (var (component, fields) in components)
            {
                Log.Debug($" -- {component}");
                foreach (var (field, value) in fields)
                {
                    Log.Debug($" -- -- {field} - {value}");
                }
            }
        }
        _data = args.Data;
    }

    public bool TryGetValue(string id, [NotNullWhen(true)] out string? value)
    {
        if (_data == null)
        {
            value = "???";
            return false;
        }
        var parts = id.Split('.');
        return _data.TryGetValue(parts[0], parts[1], parts[2], out value);
    }
}
