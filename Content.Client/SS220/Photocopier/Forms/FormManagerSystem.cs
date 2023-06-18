// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Collections.Immutable;
using Content.Shared.SS220.Photocopier.Forms;
using Content.Shared.SS220.Photocopier.Forms.FormManagerShared;

namespace Content.Client.SS220.Photocopier.Forms;

/// <summary>
/// Asks server for a complete form tree. Gives it as an immutable to photocopier's UI.
/// </summary>
public sealed class FormManager : EntitySystem
{
    private Dictionary<string, Dictionary<string, FormGroup>> _collections = new();
    private readonly ISawmill _sawmill = Logger.GetSawmill("form-manager");

    /// <summary>
    /// Provides a tree of forms, used by photocopier's UI.
    /// </summary>
    /// <returns>An immutable dictionary of collections, which are represented as immutable dictionaries of FormGroups</returns>
    public ImmutableDictionary<string, ImmutableDictionary<string, FormGroup>> GetImmutableFormsTree()
    {
        return _collections.ToImmutableDictionary(pair => pair.Key, pair => pair.Value.ToImmutableDictionary());
    }

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeNetworkEvent<PhotocopierFormsMessage>(OnRulesReceived);
        _sawmill.Debug("Requested forms from server");
        RaiseNetworkEvent(new RequestPhotocopierFormsMessage());
    }

    private void OnRulesReceived(PhotocopierFormsMessage message, EntitySessionEventArgs args)
    {
        _sawmill.Debug("Received forms from server, amount of collections: " + message.Data.Count);
        _collections = message.Data;
    }
}
