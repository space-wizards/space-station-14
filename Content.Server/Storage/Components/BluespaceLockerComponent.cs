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
    [DataField("minBluespaceLinks"), ViewVariables(VVAccess.ReadWrite)]
    public uint MinBluespaceLinks;

    /// <summary>
    /// Determines if links automatically added are restricted to the same map
    /// </summary>
    [DataField("pickLinksFromSameMap"), ViewVariables(VVAccess.ReadWrite)]
    public bool PickLinksFromSameMap;

    /// <summary>
    /// Determines if links automatically added must have ResistLockerComponent
    /// </summary>
    [DataField("pickLinksFromResistLockers"), ViewVariables(VVAccess.ReadWrite)]
    public bool PickLinksFromResistLockers = true;

    /// <summary>
    /// Determines if links automatically added are restricted to being on a station
    /// </summary>
    [DataField("pickLinksFromStationGrids"), ViewVariables(VVAccess.ReadWrite)]
    public bool PickLinksFromStationGrids = true;

    /// <summary>
    /// Determines if links automatically added are restricted to having the same access
    /// </summary>
    [DataField("pickLinksFromSameAccess"), ViewVariables(VVAccess.ReadWrite)]
    public bool PickLinksFromSameAccess = true;

    /// <summary>
    /// Determines if links automatically added are restricted to existing bluespace lockers
    /// </summary>
    [DataField("pickLinksFromBluespaceLockers"), ViewVariables(VVAccess.ReadWrite)]
    public bool PickLinksFromBluespaceLockers;

    /// <summary>
    /// Determines if links automatically added are restricted to non-bluespace lockers
    /// </summary>
    [DataField("pickLinksFromNonBluespaceLockers"), ViewVariables(VVAccess.ReadWrite)]
    public bool PickLinksFromNonBluespaceLockers = true;

    /// <summary>
    /// Determines if links automatically added get the source locker set as a target
    /// </summary>
    [DataField("autoLinksBidirectional"), ViewVariables(VVAccess.ReadWrite)]
    public bool AutoLinksBidirectional;

    /// <summary>
    /// Determines if links automatically use <see cref="AutoLinkProperties"/>
    /// </summary>
    [DataField("autoLinksUseProperties"), ViewVariables(VVAccess.ReadWrite)]
    public bool AutoLinksUseProperties;

    [DataField("usesSinceLinkClear"), ViewVariables(VVAccess.ReadWrite)]
    public int UsesSinceLinkClear;

    [DataField("bluespaceEffectMinInterval"), ViewVariables(VVAccess.ReadOnly)]
    public uint BluespaceEffectNextTime { get; set; }

    /// <summary>
    /// Determines properties of automatically created links
    /// </summary>
    [DataField("autoLinkProperties"), ViewVariables(VVAccess.ReadOnly)]
    public BluespaceLockerBehaviorProperties AutoLinkProperties = new();

    /// <summary>
    /// Determines properties of this locker
    /// </summary>
    [DataField("behaviorProperties"), ViewVariables(VVAccess.ReadOnly)]
    public BluespaceLockerBehaviorProperties BehaviorProperties = new();
}

[DataDefinition]
public partial record BluespaceLockerBehaviorProperties
{
    /// <summary>
    /// Determines if gas will be transported.
    /// </summary>
    [DataField("transportGas"), ViewVariables(VVAccess.ReadWrite)]
    public bool TransportGas { get; set; } = true;

    /// <summary>
    /// Determines if entities will be transported.
    /// </summary>
    [DataField("transportEntities"), ViewVariables(VVAccess.ReadWrite)]
    public bool TransportEntities { get; set; } = true;

    /// <summary>
    /// Determines if entities with a Mind component will be transported.
    /// </summary>
    [DataField("transportSentient"), ViewVariables(VVAccess.ReadWrite)]
    public bool TransportSentient { get; set; } = true;

    /// <summary>
    /// Determines if the the locker will act on opens.
    /// </summary>
    [DataField("actOnOpen"), ViewVariables(VVAccess.ReadWrite)]
    public bool ActOnOpen { get; set; } = true;

    /// <summary>
    /// Determines if the the locker will act on closes.
    /// </summary>
    [DataField("actOnClose"), ViewVariables(VVAccess.ReadWrite)]
    public bool ActOnClose { get; set; } = true;

    /// <summary>
    /// Delay to wait after closing before transporting
    /// </summary>
    [DataField("delay"), ViewVariables(VVAccess.ReadWrite)]
    public float Delay { get; set; }

    /// <summary>
    /// Determines if bluespace effect is show on component init
    /// </summary>
    [DataField("bluespaceEffectOnInit"), ViewVariables(VVAccess.ReadWrite)]
    public bool BluespaceEffectOnInit;

    /// <summary>
    /// Defines prototype to spawn for bluespace effect
    /// </summary>
    [DataField("bluespaceEffectPrototype"), ViewVariables(VVAccess.ReadWrite)]
    public string BluespaceEffectPrototype { get; set; } = "EffectFlashBluespace";

    /// <summary>
    /// Determines if bluespace effect is show on teleport at the source
    /// </summary>
    [DataField("bluespaceEffectOnTeleportSource"), ViewVariables(VVAccess.ReadWrite)]
    public bool BluespaceEffectOnTeleportSource { get; set; }

    /// <summary>
    /// Determines if bluespace effect is show on teleport at the target
    /// </summary>
    [DataField("bluespaceEffectOnTeleportTarget"), ViewVariables(VVAccess.ReadWrite)]
    public bool BluespaceEffectOnTeleportTarget { get; set; }

    /// <summary>
    /// Determines the minimum interval between bluespace effects
    /// </summary>
    /// <seealso cref="BluespaceEffectPrototype"/>
    [DataField("bluespaceEffectMinInterval"), ViewVariables(VVAccess.ReadWrite)]
    public double BluespaceEffectMinInterval { get; set; } = 2;

    /// <summary>
    /// Uses left before the locker is destroyed. -1 indicates infinite
    /// </summary>
    [DataField("destroyAfterUses"), ViewVariables(VVAccess.ReadWrite)]
    public int DestroyAfterUses { get; set; } = -1;

    /// <summary>
    /// Minimum number of entities that must be transported to count a use for <see cref="DestroyAfterUses"/>
    /// </summary>
    [DataField("destroyAfterUsesMinItemsToCountUse"), ViewVariables(VVAccess.ReadWrite)]
    public int DestroyAfterUsesMinItemsToCountUse { get; set; }

    /// <summary>
    /// How to destroy the locker after it runs out of uses
    /// </summary>
    [DataField("destroyType"), ViewVariables(VVAccess.ReadWrite)]
    public BluespaceLockerDestroyType DestroyType { get; set; } = BluespaceLockerDestroyType.Delete;

    /// <summary>
    /// Uses left before the lockers links are cleared. -1 indicates infinite
    /// </summary>
    [DataField("clearLinksEvery"), ViewVariables(VVAccess.ReadWrite)]
    public int ClearLinksEvery { get; set; } = -1;

    /// <summary>
    /// Determines if cleared links have their component removed
    /// </summary>
    [DataField("clearLinksDebluespaces"), ViewVariables(VVAccess.ReadWrite)]
    public bool ClearLinksDebluespaces { get; set; }

    /// <summary>
    /// Links will not be valid if they're not bidirectional
    /// </summary>
    [DataField("invalidateOneWayLinks"), ViewVariables(VVAccess.ReadWrite)]
    public bool InvalidateOneWayLinks { get; set; }
}

[Flags]
public enum BluespaceLockerDestroyType
{
    Delete,
    DeleteComponent,
    Explode,
}
