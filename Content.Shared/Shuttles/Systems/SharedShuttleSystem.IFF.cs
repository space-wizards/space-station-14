using Content.Shared.Shuttles.Components;
using JetBrains.Annotations;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.Systems;

public abstract partial class SharedShuttleSystem
{
    private void InitializeIFF()
    {
        SubscribeLocalEvent<IFFComponent, ComponentGetState>(OnIFFGetState);
        SubscribeLocalEvent<IFFComponent, ComponentHandleState>(OnIFFHandleState);
    }

    [PublicAPI]
    public void SetColor(EntityUid gridUid, Color color, IFFComponent? component = null)
    {
        component ??= EnsureComp<IFFComponent>(gridUid);

        if (component.Color.Equals(color))
            return;

        component.Color = color;
        Dirty(component);
    }

    [PublicAPI]
    public void AddFlag(EntityUid gridUid, IFFFlags flags, IFFComponent? component = null)
    {
        component ??= EnsureComp<IFFComponent>(gridUid);

        if ((component.Flags & flags) == flags)
            return;

        component.Flags |= flags;
        Dirty(component);
    }

    [PublicAPI]
    public void RemoveFlag(EntityUid gridUid, IFFFlags flags, IFFComponent? component = null)
    {
        if (!TryComp(gridUid, out component))
            return;

        if ((component.Flags & flags) == 0x0)
            return;

        component.Flags &= ~flags;
        Dirty(component);
    }

    private void OnIFFHandleState(EntityUid uid, IFFComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not IFFComponentState state)
            return;

        component.Flags = state.Flags;
        component.Color = state.Color;
    }

    private void OnIFFGetState(EntityUid uid, IFFComponent component, ref ComponentGetState args)
    {
        args.State = new IFFComponentState()
        {
            Flags = component.Flags,
            Color = component.Color,
        };
    }

    [Serializable, NetSerializable]
    private sealed class IFFComponentState : ComponentState
    {
        public IFFFlags Flags;
        public Color Color;
    }
}
