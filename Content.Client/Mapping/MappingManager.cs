using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.Decals;
using Content.Shared.Mapping;
using Content.Shared.Maps;
using Robust.Client.UserInterface;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Client.Mapping;

public sealed class MappingManager : IPostInjectInit
{
    [Dependency] private readonly IFileDialogManager _file = default!;
    [Dependency] private readonly IClientNetManager _net = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private Stream? _saveStream;
    private MappingMapDataMessage? _mapData;
    private List<IPrototype>? _favoritePrototypes;

    public event Action<List<IPrototype>>? OnFavoritePrototypesLoaded;

    public void PostInject()
    {
        _net.RegisterNetMessage<MappingSaveMapMessage>();
        _net.RegisterNetMessage<MappingSaveMapErrorMessage>(OnSaveError);
        _net.RegisterNetMessage<MappingMapDataMessage>(OnMapData);
        _net.RegisterNetMessage<MappingFavoritesDataMessage>(OnFavoritesData);
        _net.RegisterNetMessage<MappingFavoritesSaveMessage>();
    }

    private void OnSaveError(MappingSaveMapErrorMessage message)
    {
        _saveStream?.DisposeAsync();
        _saveStream = null;
    }

    private async void OnMapData(MappingMapDataMessage message)
    {
        if (_saveStream == null)
        {
            _mapData = message;
            return;
        }

        await _saveStream.WriteAsync(Encoding.ASCII.GetBytes(message.Yml));
        await _saveStream.DisposeAsync();

        _saveStream = null;
        _mapData = null;
    }

    private void OnFavoritesData(MappingFavoritesDataMessage message)
    {
        _favoritePrototypes = new List<IPrototype>();

        foreach (var prototype in message.PrototypeIDs)
        {
            if (_prototypeManager.TryIndex<EntityPrototype>(prototype, out var entity))
                _favoritePrototypes.Add(entity);
            else if (_prototypeManager.TryIndex<ContentTileDefinition>(prototype, out var tile))
                _favoritePrototypes.Add(tile);
            else if (_prototypeManager.TryIndex<DecalPrototype>(prototype, out var decal))
                _favoritePrototypes.Add(decal);
        }

        OnFavoritePrototypesLoaded?.Invoke(_favoritePrototypes);
    }

    public async Task SaveMap()
    {
        if (_saveStream != null)
            await _saveStream.DisposeAsync();

        var request = new MappingSaveMapMessage();
        _net.ClientSendMessage(request);

        var path = await _file.SaveFile();
        if (path is not { fileStream: var stream })
            return;

        if (_mapData != null)
        {
            await stream.WriteAsync(Encoding.ASCII.GetBytes(_mapData.Yml));
            _mapData = null;
            await stream.FlushAsync();
            await stream.DisposeAsync();
            return;
        }

        _saveStream = stream;
    }

    public void SaveFavorites(List<MappingPrototype> prototypes)
    {
        // TODO: that doesnt save null prototypes (mapping templates and abstract parents)
        var msg = new MappingFavoritesSaveMessage()
        {
            PrototypeIDs = prototypes
                .FindAll(p => p.Prototype != null)
                .Select(p => p.Prototype!.ID)
                .ToList(),
        };
        _net.ClientSendMessage(msg);
    }

    public void LoadFavorites()
    {
        var request = new MappingFavoritesLoadMessage();
        _net.ClientSendMessage(request);
    }
}
