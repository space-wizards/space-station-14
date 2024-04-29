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

        _data = args.Data;
    }

    public bool TryGetValue(string prototype, string component, string field, out object? value)
    {
        if (_data == null)
        {
            value = null;
            return false;
        }
        return _data.TryGetValue(prototype, component, field, out value);
    }
}
