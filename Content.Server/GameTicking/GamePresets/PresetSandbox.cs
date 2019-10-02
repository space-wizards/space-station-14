using Content.Server.Interfaces.GameTicking;
using Content.Server.Sandbox;
using Robust.Shared.IoC;

namespace Content.Server.GameTicking.GamePresets
{
    public sealed class PresetSandbox : GamePreset
    {
#pragma warning disable 649
        [Dependency] private readonly ISandboxManager _sandboxManager;
#pragma warning restore 649

        public override void Start()
        {
            _sandboxManager.IsSandboxEnabled = true;
        }
    }
}
