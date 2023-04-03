using Content.Shared.Shuttles.Components;
using JetBrains.Annotations;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.Systems;

public abstract partial class SharedShuttleSystem
{
    /*
     * Handles the label visibility on radar controls. This can be hiding the label or applying other effects.
     */

    private void InitializeIFF()
    {
        SubscribeLocalEvent<IFFComponent, ComponentGetState>(OnIFFGetState);
        SubscribeLocalEvent<IFFComponent, ComponentHandleState>(OnIFFHandleState);
    }

    protected virtual void UpdateIFFInterfaces(EntityUid gridUid, IFFComponent component) {}

    /// <summary>
    /// Sets the color for this grid to appear as on radar.
    /// </summary>
    [PublicAPI]
    public void SetIFFColor(EntityUid gridUid, Color color, IFFComponent? component = null)
    {
        component ??= EnsureComp<IFFComponent>(gridUid);

        if (component.Color.Equals(color))
            return;

        component.Color = color;
        Dirty(component);
        UpdateIFFInterfaces(gridUid, component);
    }

    [PublicAPI]
    public void AddIFFFlag(EntityUid gridUid, IFFFlags flags, IFFComponent? component = null)
    {
        component ??= EnsureComp<IFFComponent>(gridUid);

        if ((component.Flags & flags) == flags)
            return;

        component.Flags |= flags;
        Dirty(component);
        UpdateIFFInterfaces(gridUid, component);
    }

    [PublicAPI]
    public void RemoveIFFFlag(EntityUid gridUid, IFFFlags flags, IFFComponent? component = null)
    {
        if (!Resolve(gridUid, ref component, false))
            return;

        if ((component.Flags & flags) == 0x0)
            return;

        component.Flags &= ~flags;
        Dirty(component);
        UpdateIFFInterfaces(gridUid, component);
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
