using Content.Shared.Shuttles.Components;
using JetBrains.Annotations;
using Robust.Shared.Map.Components;

namespace Content.Shared.Shuttles.Systems;

public abstract partial class SharedShuttleSystem
{
    /*
     * Handles the label visibility on radar controls. This can be hiding the label or applying other effects.
     */

    protected virtual void UpdateIFFInterfaces(EntityUid gridUid, IFFComponent component) {}

    public Color GetIFFColor(EntityUid gridUid, bool self = false, IFFComponent? component = null)
    {
        if (self)
        {
            return IFFComponent.SelfColor;
        }

        if (!Resolve(gridUid, ref component, false))
        {
            return IFFComponent.IFFColor;
        }

        return component.Color;
    }

    public string? GetIFFLabel(EntityUid gridUid, bool self = false, IFFComponent? component = null)
    {
        var entName = MetaData(gridUid).EntityName;

        if (self)
        {
            return entName;
        }

        if (Resolve(gridUid, ref component, false) && (component.Flags & (IFFFlags.HideLabel | IFFFlags.Hide)) != 0x0)
        {
            return null;
        }

        return string.IsNullOrEmpty(entName) ? Loc.GetString("shuttle-console-unknown") : entName;
    }

    /// <summary>
    /// Checks if the GridUid has the specified IFF-Flag.
    /// </summary>
    /// <param name="gridUid">The grid whose IFF-Flags are being checked.</param>
    /// <param name="requiredFlag">The required IFF-Flag.</param>
    /// <param name="component">The IFF component of the grid.</param>
    /// <returns>
    /// Returns true if the grid has the required IFF-Flag, otherwise false.
    /// </returns>
    /// <remarks>
    /// Returns false if the Uid is not a grid and the required IFF-Flag is anything but <see cref="IFFFlags.None"/>, otherwise true.
    /// </remarks>
    [PublicAPI]
    public bool HasIFFFlag(EntityUid gridUid, IFFFlags requiredFlag, IFFComponent? component = null)
    {
        // Check if it's a grid.
        if (!HasComp<MapGridComponent>(gridUid))
            return requiredFlag == IFFFlags.None;

        component ??= EnsureComp<IFFComponent>(gridUid);

        return component.Flags.HasFlag(requiredFlag);
    }

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
        Dirty(gridUid, component);
        UpdateIFFInterfaces(gridUid, component);
    }

    [PublicAPI]
    public void AddIFFFlag(EntityUid gridUid, IFFFlags flags, IFFComponent? component = null)
    {
        component ??= EnsureComp<IFFComponent>(gridUid);

        if ((component.Flags & flags) == flags)
            return;

        component.Flags |= flags;
        Dirty(gridUid, component);
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
        Dirty(gridUid, component);
        UpdateIFFInterfaces(gridUid, component);
    }
}
