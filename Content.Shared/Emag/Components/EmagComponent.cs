using Content.Shared.Emag.Systems;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization;
using Content.Shared.Silicons.Laws;
using Content.Shared.Radio; //#Starlight

namespace Content.Shared.Emag.Components;

[Access(typeof(EmagSystem))]
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class EmagComponent : Component
{
    /// <summary>
    /// The tag that marks an entity as immune to emags
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public ProtoId<TagPrototype> EmagImmuneTag = "EmagImmune";

    /// <summary>
    /// What type of emag effect this device will do
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public EmagType EmagType = EmagType.Interaction;

    /// <summary>
    /// What sound should the emag play when used
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public SoundSpecifier EmagSound = new SoundCollectionSpecifier("sparks");

    //#region Starlight
    /// <summary>
    /// should this emag also destroy the transponder
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public bool DestroyTransponder = false;

    /// <summary>
    /// What lawset should borgs get when emagged. note. fully replaces the lawset and prevents the "only x and those they designate are crew" and secrecy laws.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public ProtoId<SiliconLawsetPrototype>? Lawset = null;

    /// <summary>
    /// What components should be added to a borg chassis when emagged
    /// </summary>
    [DataField]
    public ComponentRegistry? Components = null;

    /// <summary>
    /// What radio channels should be added to a emagged borg chassis
    /// </summary>
    [DataField]
    public HashSet<ProtoId<RadioChannelPrototype>> ChannelAdd = ["Syndicate"];
    //#endregion Starlight
}
