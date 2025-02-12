namespace Content.Server.Storage.Components;

[RegisterComponent]
public sealed partial class BluespaceLockerComponent : Component
{
    /// <summary>
    /// If length > 0, when something is added to the storage, it will instead be teleported to a random storage
    /// from the list and the other storage will be opened.
    /// </summary>
    [DataField("bluespaceLinks"), ViewVariables(VVAccess.ReadOnly)]
    public HashSet<EntityUid> BluespaceLinks = new();

    /// <summary>
    /// Each time the system attempts to get a link, it will link additional lockers to ensure the minimum amount
    /// are linked.
    /// </summary>
    [DataField, ViewVariables]
    public uint MinBluespaceLinks;

    /// <summary>
    /// Determines if links automatically added are restricted to the same map
    /// </summary>
    [DataField, ViewVariables]
    public bool PickLinksFromSameMap;

    /// <summary>
    /// Determines if links automatically added must have ResistLockerComponent
    /// </summary>
    [DataField, ViewVariables]
    public bool PickLinksFromResistLockers = true;

    /// <summary>
    /// Determines if links automatically added are restricted to being on a station
    /// </summary>
    [DataField, ViewVariables]
    public bool PickLinksFromStationGrids = true;

    /// <summary>
    /// Determines if links automatically added are restricted to having the same access
    /// </summary>
    [DataField, ViewVariables]
    public bool PickLinksFromSameAccess = true;

    /// <summary>
    /// Determines if links automatically added are restricted to existing bluespace lockers
    /// </summary>
    [DataField, ViewVariables]
    public bool PickLinksFromBluespaceLockers;

    /// <summary>
    /// Determines if links automatically added are restricted to non-bluespace lockers
    /// </summary>
    [DataField, ViewVariables]
    public bool PickLinksFromNonBluespaceLockers = true;

    /// <summary>
    /// Determines if links automatically added get the source locker set as a target
    /// </summary>
    [DataField, ViewVariables]
    public bool AutoLinksBidirectional;

    /// <summary>
    /// Determines if links automatically use <see cref="AutoLinkProperties"/>
    /// </summary>
    [DataField, ViewVariables]
    public bool AutoLinksUseProperties;

    [DataField, ViewVariables]
    public int UsesSinceLinkClear;

    [DataField("bluespaceEffectMinInterval"), ViewVariables(VVAccess.ReadOnly)]
    public uint BluespaceEffectNextTime { get; set; }

    /// <summary>
    /// Determines properties of automatically created links
    /// </summary>
    [DataField, ViewVariables]
    public BluespaceLockerBehaviorProperties AutoLinkProperties = new();

    /// <summary>
    /// Determines properties of this locker
    /// </summary>
    [DataField, ViewVariables]
    public BluespaceLockerBehaviorProperties BehaviorProperties = new();
}

[DataDefinition]
public partial record BluespaceLockerBehaviorProperties
{
    /// <summary>
    /// Determines if gas will be transported.
    /// </summary>
    [DataField, ViewVariables]
    public bool TransportGas { get; set; } = true;

    /// <summary>
    /// Determines if entities will be transported.
    /// </summary>
    [DataField, ViewVariables]
    public bool TransportEntities { get; set; } = true;

    /// <summary>
    /// Determines if entities with a Mind component will be transported.
    /// </summary>
    [DataField, ViewVariables]
    public bool TransportSentient { get; set; } = true;

    /// <summary>
    /// Determines if the the locker will act on opens.
    /// </summary>
    [DataField, ViewVariables]
    public bool ActOnOpen { get; set; } = true;

    /// <summary>
    /// Determines if the the locker will act on closes.
    /// </summary>
    [DataField, ViewVariables]
    public bool ActOnClose { get; set; } = true;

    /// <summary>
    /// Delay to wait after closing before transporting
    /// </summary>
    [DataField, ViewVariables]
    public float Delay { get; set; }

    /// <summary>
    /// Determines if bluespace effect is show on component init
    /// </summary>
    [DataField, ViewVariables]
    public bool BluespaceEffectOnInit;

    /// <summary>
    /// Defines prototype to spawn for bluespace effect
    /// </summary>
    [DataField, ViewVariables]
    public string BluespaceEffectPrototype { get; set; } = "EffectFlashBluespace";

    /// <summary>
    /// Determines if bluespace effect is show on teleport at the source
    /// </summary>
    [DataField, ViewVariables]
    public bool BluespaceEffectOnTeleportSource { get; set; }

    /// <summary>
    /// Determines if bluespace effect is show on teleport at the target
    /// </summary>
    [DataField, ViewVariables]
    public bool BluespaceEffectOnTeleportTarget { get; set; }

    /// <summary>
    /// Determines the minimum interval between bluespace effects
    /// </summary>
    /// <seealso cref="BluespaceEffectPrototype"/>
    [DataField, ViewVariables]
    public double BluespaceEffectMinInterval { get; set; } = 2;

    /// <summary>
    /// Uses left before the locker is destroyed. -1 indicates infinite
    /// </summary>
    [DataField, ViewVariables]
    public int DestroyAfterUses { get; set; } = -1;

    /// <summary>
    /// Minimum number of entities that must be transported to count a use for <see cref="DestroyAfterUses"/>
    /// </summary>
    [DataField, ViewVariables]
    public int DestroyAfterUsesMinItemsToCountUse { get; set; }

    /// <summary>
    /// How to destroy the locker after it runs out of uses
    /// </summary>
    [DataField, ViewVariables]
    public BluespaceLockerDestroyType DestroyType { get; set; } = BluespaceLockerDestroyType.Delete;

    /// <summary>
    /// Uses left before the lockers links are cleared. -1 indicates infinite
    /// </summary>
    [DataField, ViewVariables]
    public int ClearLinksEvery { get; set; } = -1;

    /// <summary>
    /// Determines if cleared links have their component removed
    /// </summary>
    [DataField, ViewVariables]
    public bool ClearLinksDebluespaces { get; set; }

    /// <summary>
    /// Links will not be valid if they're not bidirectional
    /// </summary>
    [DataField, ViewVariables]
    public bool InvalidateOneWayLinks { get; set; }
}

[Flags]
public enum BluespaceLockerDestroyType
{
    Delete,
    DeleteComponent,
    Explode,
}
