using Content.Server.GameTicking;
using Robust.Server.GameObjects;

namespace Content.Server.CrewManifest;

public sealed class CrewManifestSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

    public override void Initialize()
    {}


}


