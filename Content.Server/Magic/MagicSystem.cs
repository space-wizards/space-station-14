using Content.Server.Magic.Events;

namespace Content.Server.Magic;

public sealed class MagicSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MagicComponent>;

    }

}
