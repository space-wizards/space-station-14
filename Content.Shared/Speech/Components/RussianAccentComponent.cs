using Content.Shared.Speech.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Speech.Components;

/// <summary>
/// ЯussiДи! Becomes incomprehensible to read for anyone who actually knows cyrillic.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(RussianAccentSystem))]
public sealed partial class RussianAccentComponent : BaseAccentComponent;
