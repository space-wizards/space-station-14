using Robust.Shared.Utility;

namespace Content.Server.Shuttles.Components;

/// <summary>
/// If added to an airlock will try to autofill an escape pod onto it on MapInit
/// </summary>
[RegisterComponent]
public sealed class EscapePodFillComponent : Component
{
    [DataField("path")] public ResourcePath Path = new("/Maps/Shuttles/escape_pod.yml");
}
