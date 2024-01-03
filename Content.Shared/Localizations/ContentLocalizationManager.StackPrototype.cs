using System.Collections.Concurrent;

namespace Content.Shared.Localizations
{
    public record StackPrototypeLocalization(string? Name);

    public sealed partial class ContentLocalizationManager
    {
        private readonly ConcurrentDictionary<string, StackPrototypeLocalization> _stackPrototypeLocalizations = new();

        public StackPrototypeLocalization GetStackPrototypeLocalization(string prototypeId)
        {
            return _stackPrototypeLocalizations.GetOrAdd(prototypeId, _ => CalcStackPrototypeLocalization(prototypeId));
        }

        private StackPrototypeLocalization CalcStackPrototypeLocalization(string prototypeId)
        {
            return new StackPrototypeLocalization(
                _loc.TryGetString($"stack-{prototypeId}", out var name) ? name : null);
        }
    }
}
