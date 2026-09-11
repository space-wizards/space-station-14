namespace Content.Shared.Xenoborgs.Components;

/// <summary>
/// Defines what is a xenoborg for the intentions of the xenoborg rule. if all xenoborg cores are destroyed. all xenoborgs will self-destruct.
///
/// It's also used by the mothership core
/// </summary>
[RegisterComponent]
public sealed partial class XenoborgComponent : Component;