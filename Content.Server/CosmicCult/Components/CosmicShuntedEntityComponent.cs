using Content.Shared.CosmicCult.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.CosmicCult.Components;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class CosmicShuntedEntityComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))] [AutoPausedField]
    public TimeSpan ExitVoidTime;

    [DataField]
    public bool ReadyToReturn;

    [DataField]
    public bool ConvertOnReturn;

    [DataField]
    public EntityUid OriginalBody;

    public EntityUid ShuntCaster;

    public EntityUid WispGrabber;

    /// <summary>
    /// Blacklist of the components that prevent a victim from being converted. This blacklist copies the blacklist on the CosmicActionShuntComponent.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;
}
