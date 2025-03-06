using Content.Shared.Stealth.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Robust.Shared.Utility;

namespace Content.Shared.StatusIcon;

/// <summary>
/// A data structure that holds relevant
/// information for status icons.
/// </summary>
[Virtual, DataDefinition]
public partial class StatusIconData : IComparable<StatusIconData>
{
    /// <summary>
    /// The icon that's displayed on the entity.
    /// </summary>
    [DataField(required: true)]
    public SpriteSpecifier Icon = default!;

    /// <summary>
    /// A priority for the order in which the icons will be displayed.
    /// </summary>
    [DataField]
    public int Priority = 10;

    /// <summary>
    /// Whether or not to hide the icon to ghosts
    /// </summary>
    [DataField]
    public bool VisibleToGhosts = true;

    /// <summary>
    /// Whether or not to hide the icon when we are inside a container like a locker or a crate.
    /// </summary>
    [DataField]
    public bool HideInContainer = true;

    /// <summary>
    /// Whether or not to hide the icon when the entity has an active <see cref="StealthComponent"/>
    /// </summary>
    [DataField]
    public bool HideOnStealth = true;

    /// <summary>
    /// Specifies what entities and components/tags this icon can be shown to.
    /// </summary>
    [DataField]
    public EntityWhitelist? ShowTo;

    /// <summary>
    /// A preference for where the icon will be displayed. None | Left | Right
    /// </summary>
    [DataField]
    public StatusIconLocationPreference LocationPreference = StatusIconLocationPreference.None;

    /// <summary>
    /// The layer the icon is displayed on. Mod is drawn above Base. Base | Mod
    /// </summary>
    [DataField]
    public StatusIconLayer Layer = StatusIconLayer.Base;

    /// <summary>
    /// Offset of the status icon, up and down only.
    /// </summary>
    [DataField]
    public int Offset = 0;

    /// <summary>
    /// Sets if the icon should be rendered with or without the effect of lighting.
    /// </summary>
    [DataField]
    public bool IsShaded = false;
    public int CompareTo(StatusIconData? other)
    {
        return Priority.CompareTo(other?.Priority ?? int.MaxValue);
    }
}

/// <summary>
/// <see cref="StatusIconData"/> but in new convenient prototype form!
/// </summary>
public abstract partial class StatusIconPrototype : StatusIconData, IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;
}

/// <summary>
/// StatusIcons for showing jobs on the sec HUD
/// </summary>
[Prototype]
public sealed partial class JobIconPrototype : StatusIconPrototype, IInheritingPrototype
{
    /// <inheritdoc />
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<JobIconPrototype>))]
    public string[]? Parents { get; private set; }

    /// <inheritdoc />
    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; private set; }

    /// <summary>
    /// Name of the icon used for menu tooltips.
    /// </summary>
    [DataField]
    public string JobName { get; private set; } = string.Empty;

    [ViewVariables(VVAccess.ReadOnly)]
    public string LocalizedJobName => Loc.GetString(JobName);

    /// <summary>
    /// Should the agent ID or ID card console be able to use this job icon?
    /// </summary>
    [DataField]
    public bool AllowSelection = true;
}

/// <summary>
/// StatusIcons for the med HUD
/// </summary>
[Prototype]
public sealed partial class HealthIconPrototype : StatusIconPrototype, IInheritingPrototype
{
    /// <inheritdoc />
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<HealthIconPrototype>))]
    public string[]? Parents { get; private set; }

    /// <inheritdoc />
    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; private set; }
}

/// <summary>
/// StatusIcons for the beer goggles and fried onion goggles
/// </summary>
[Prototype]
public sealed partial class SatiationIconPrototype : StatusIconPrototype, IInheritingPrototype
{
    /// <inheritdoc />
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<SatiationIconPrototype>))]
    public string[]? Parents { get; private set; }

    /// <inheritdoc />
    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; private set; }
}

/// <summary>
/// StatusIcons for showing the wanted status on the sec HUD
/// </summary>
[Prototype]
public sealed partial class SecurityIconPrototype : StatusIconPrototype, IInheritingPrototype
{
    /// <inheritdoc />
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<SecurityIconPrototype>))]
    public string[]? Parents { get; private set; }

    /// <inheritdoc />
    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; private set; }
}

/// <summary>
/// StatusIcons for faction membership
/// </summary>
[Prototype]
public sealed partial class FactionIconPrototype : StatusIconPrototype, IInheritingPrototype
{
    /// <inheritdoc />
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<FactionIconPrototype>))]
    public string[]? Parents { get; private set; }

    /// <inheritdoc />
    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; private set; }
}

/// <summary>
/// StatusIcons for debugging purposes
/// </summary>
[Prototype]
public sealed partial class DebugIconPrototype : StatusIconPrototype, IInheritingPrototype
{
    /// <inheritdoc />
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<DebugIconPrototype>))]
    public string[]? Parents { get; private set; }

    /// <inheritdoc />
    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; private set; }
}

/// <summary>
/// StatusIcons for the SSD indicator
/// </summary>
[Prototype]
public sealed partial class SsdIconPrototype : StatusIconPrototype, IInheritingPrototype
{
    /// <inheritdoc />
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<SsdIconPrototype>))]
    public string[]? Parents { get; private set; }

    /// <inheritdoc />
    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; private set; }
}

[Serializable, NetSerializable]
public enum StatusIconLocationPreference : byte
{
    None,
    Left,
    Right,
}

public enum StatusIconLayer : byte
{
    Base,
    Mod,
}
