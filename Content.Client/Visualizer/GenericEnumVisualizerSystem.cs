using System.Linq;
using Robust.Client.GameObjects;
using Robust.Shared.Reflection;

namespace Content.Client.Visualizer;

public sealed class GenericEnumVisualizerSystem : VisualizerSystem<GenericEnumVisualizerComponent>
{
    [Dependency] private readonly IReflectionManager _refMan = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GenericEnumVisualizerComponent, ComponentInit>(OnInit);
    }

    private void OnInit(EntityUid uid, GenericEnumVisualizerComponent comp, ComponentInit args)
    {
        object ResolveRef(string raw)
        {
            if (_refMan.TryParseEnumReference(raw, out var @enum))
            {
                return @enum;
            }
            else
            {
                Logger.WarningS("c.c.v.genum", $"Unable to convert enum reference: {raw}");
            }

            return raw;
        }

        // It's important that this conversion be done here so that it may "fail-fast".
        comp.Key = (Enum) ResolveRef(comp.KeyRaw);
        comp.States = comp.StatesRaw.ToDictionary(kvp => ResolveRef(kvp.Key), kvp => kvp.Value);
    }

    protected override void OnAppearanceChange(EntityUid uid, GenericEnumVisualizerComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;
        if(!AppearanceSystem.TryGetData(uid, comp.Key, out object? status, args.Component))
            return;
        if(!comp.States.TryGetValue(status, out var val))
            return;
        args.Sprite.LayerSetState(comp.Layer, val);
    }
}
