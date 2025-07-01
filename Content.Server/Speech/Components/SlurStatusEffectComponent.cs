namespace Content.Server.Speech.Components;

/// <summary>
/// This is used by a status effect entity to apply the <see cref="SlurredAccentComponent"/> to an entity.
/// It is serverside only since chat accents are purely server side and probably will always be purely server side.
/// </summary>
[RegisterComponent]
public sealed partial class SlurStatusEffectComponent : Component
{

}
