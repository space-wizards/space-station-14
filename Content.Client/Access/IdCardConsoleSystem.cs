using Content.Shared.Access;
using JetBrains.Annotations;

namespace Content.Client.Access
{
    [UsedImplicitly]
    public sealed class IdCardConsoleSystem : SharedIdCardConsoleSystem
    {
        // one day, maybe bound user interfaces can be shared too.
        // then this doesnt have to be like this.
        // I hate this.
    }
}
