using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Preferences.Loadouts.Effects;

/// <summary>
/// Uses a <see cref="LoadoutEffectGroupPrototype"/> prototype as a singular effect that can be re-used.
/// </summary>
public sealed partial class GroupLoadoutEffect : LoadoutEffect
{
    [DataField(required: true)]
    public ProtoId<LoadoutEffectGroupPrototype> Proto;

    public override bool Validate(RoleLoadout loadout, ICommonSession session, IDependencyCollection collection, [NotNullWhen(false)] out FormattedMessage? reason)
    {
        var effectsProto = collection.Resolve<IPrototypeManager>().Index(Proto);

        foreach (var effect in effectsProto.Effects)
        {
            if (!effect.Validate(loadout, session, collection, out reason))
                return false;
        }

        reason = null;
        return true;
    }
}
