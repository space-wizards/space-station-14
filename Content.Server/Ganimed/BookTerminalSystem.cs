using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Database;
using Content.Server.Ganimed.Components;
using Content.Shared.Ganimed;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.GameObjects;
using Content.Shared.Containers.ItemSlots;
using Content.Server.Paper;
using Content.Shared.Paper;
using Robust.Shared.Random;
using Content.Shared.Tag;
using Content.Server.Labels.Components;
using System.Threading.Tasks;
using Robust.Shared.Asynchronous;
using Content.Shared.Ganimed;

namespace Content.Server.Ganimed
{
    [UsedImplicitly]
	public sealed class BookTerminalSystem : EntitySystem
    {
        [Dependency] private readonly AudioSystem _audioSystem = default!;
        [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly MetaDataSystem _metaData = default!;
        [Dependency] private readonly PaperSystem _paperSystem = default!;
		[Dependency] private readonly IRobustRandom _random = default!;
		[Dependency] private readonly ITaskManager _task = default!;
		[Dependency] private readonly TagSystem _tag = default!;
		
		private readonly List<Task> _pendingSaveTasks = new();
		public readonly List<SharedBookTerminalEntry> bookTerminalEntries = new();
		
		public override void Initialize()
        {
            base.Initialize();
			SubscribeLocalEvent<BookTerminalComponent, ComponentStartup>(SubscribeUpdateUiState);
            SubscribeLocalEvent<BookTerminalComponent, EntInsertedIntoContainerMessage>(SubscribeUpdateUiState);
            SubscribeLocalEvent<BookTerminalComponent, EntRemovedFromContainerMessage>(SubscribeUpdateUiState);
            SubscribeLocalEvent<BookTerminalComponent, BoundUIOpenedEvent>(SubscribeUpdateUiState);
			
            SubscribeLocalEvent<BookTerminalComponent, BookTerminalClearContainerMessage>(OnClearContainerMessage);
			SubscribeLocalEvent<BookTerminalComponent, BookTerminalUploadMessage>(OnUploadMessage);
            SubscribeLocalEvent<BookTerminalComponent, BookTerminalPrintBookMessage>(OnPrintBookMessage);
        }
		
		private void SubscribeUpdateUiState<T>(Entity<BookTerminalComponent> ent, ref T ev)
        {
            UpdateUiState(ent);
			RefreshBookContent();
        }
		
		private void UpdateUiState(Entity<BookTerminalComponent> bookTerminal)
        {
            var bookContainer = _itemSlotsSystem.GetItemOrNull(bookTerminal, "bookSlot");
			var bookName = bookContainer is not null ? Name(bookContainer.Value) : null;
			var bookDescription = bookContainer is not null ? Description(bookContainer.Value) : null;

            var state = new BookTerminalBoundUserInterfaceState(bookName, bookDescription, GetNetEntity(bookContainer), bookTerminalEntries);
            _userInterfaceSystem.TrySetUiState(bookTerminal, BookTerminalUiKey.Key, state);
        }
		
		private void OnClearContainerMessage(Entity<BookTerminalComponent> bookTerminal, ref BookTerminalClearContainerMessage message)
        {
            var bookContainer = _itemSlotsSystem.GetItemOrNull(bookTerminal, "bookSlot");
            if (bookContainer is not {Valid: true})
                return;
			
			ClearContent(bookContainer.Value);
			
			UpdateUiState(bookTerminal);
            ClickSound(bookTerminal);
        }
		
		private void OnUploadMessage(Entity<BookTerminalComponent> bookTerminal, ref BookTerminalUploadMessage message)
        {
            var bookContainer = _itemSlotsSystem.GetItemOrNull(bookTerminal, "bookSlot");
            if (bookContainer is not {Valid: true})
                return;
			
			UploadContent(bookContainer.Value);
			
			UpdateUiState(bookTerminal);
            ClickSound(bookTerminal);
        }
		
		private void OnPrintBookMessage(Entity<BookTerminalComponent> bookTerminal, ref BookTerminalPrintBookMessage message)
        {
            var bookContainer = _itemSlotsSystem.GetItemOrNull(bookTerminal, "bookSlot");
            if (bookContainer is not {Valid: true})
                return;
			if (bookTerminalEntries.Count() < 1)
				return;
			
			RefreshBookContent();
			ClearContent(bookContainer.Value);
			SetContent(bookContainer.Value, message.BookEntry);

            UpdateUiState(bookTerminal);
            ClickSound(bookTerminal);
        }
		
		private void ClearContent(EntityUid? item)
		{
			if (item is null)
				return;
			
			if (EntityManager.TryGetComponent(item.Value, out PaperComponent? paperComp))
			{
				paperComp.StampedBy = new List<StampDisplayInfo>();
				paperComp.StampState = null;
				_paperSystem.SetContent(item.Value, "", paperComp, false);
				_paperSystem.UpdateStampState(item.Value, paperComp);
			}
			
			if (EntityManager.TryGetComponent<MetaDataComponent>(item.Value, out var metadata))
			{
				
				var newName = Loc.GetString("book-terminal-unknown-name-blank");
				var newDesc = Loc.GetString("book-terminal-unknown-description-blank");
				
				if (_tag.HasTag(item.Value, "Book"))
				{
					newName = Loc.GetString("book-terminal-book-name-blank");
					newDesc = Loc.GetString("book-terminal-book-description-blank");
				} else if (_tag.HasTag(item.Value, "Document"))
				{
					newName = Loc.GetString("book-terminal-paper-name-blank");
					newDesc = Loc.GetString("book-terminal-paper-description-blank");
				}
				
				_metaData.SetEntityName(item.Value, newName, metadata);
				_metaData.SetEntityDescription(item.Value, newDesc, metadata);
			}
			
			RemComp<LabelComponent>(item.Value);
		}
		
		private void SetContent(EntityUid? item, SharedBookTerminalEntry bookEntry)
		{
			if (item is null)
				return;
			
			var paperComp = EnsureComp<PaperComponent>(item.Value);
			var metadata = EnsureComp<MetaDataComponent>(item.Value);
			
			_metaData.SetEntityName(item.Value, bookEntry.Name, metadata);
			_metaData.SetEntityDescription(item.Value, bookEntry.Description, metadata);
			_paperSystem.SetContent(item.Value, bookEntry.Content, paperComp, false);
			
			foreach (var stampEntry in bookEntry.StampedBy)
			{
				var stampInfo = new StampDisplayInfo {
                StampedName = stampEntry.Name,
                StampedColor = Color.FromHex(stampEntry.Color)
				};
			
				_paperSystem.TryStamp(item.Value, stampInfo, "paper_stamp-void", paperComp);
			}
			
			paperComp.StampState = bookEntry.StampState != "" ? bookEntry.StampState : null;
			_paperSystem.UpdateStampState(item.Value, paperComp);
		}
		
		public async void RefreshBookContent()
		{
			var db = IoCManager.Resolve<IServerDbManager>();
			var entries = await db.GetBookTerminalEntriesAsync();
			
			bookTerminalEntries.Clear();
			
			foreach (var entry in entries)
			{
				var convStampedBy = new List<SharedStampedData>();
				foreach (var stampEntry in entry.StampedBy)
					convStampedBy.Add(new SharedStampedData(stampEntry.Id, stampEntry.Name, stampEntry.Color));
				
				bookTerminalEntries.Add(new SharedBookTerminalEntry(entry.Id, entry.Name, entry.Description, entry.Content, convStampedBy, entry.StampState));
			}
		}
		
		public SharedBookTerminalEntry? RetrieveBookContent(int id)
		{
			return bookTerminalEntries.Find(entry => entry.Id == id);
		}
		
		private async void TrackPending(Task task)
		{
			_pendingSaveTasks.Add(task);

			try
			{
				await task;
			}
			finally
			{
				_pendingSaveTasks.Remove(task);
			}
		}
		
		private void UploadContent(EntityUid? item)
		{
			if (item is null)
				return;
			
			var paperComp = EnsureComp<PaperComponent>(item.Value);
			var metadata = EnsureComp<MetaDataComponent>(item.Value);
			
			//var proto = new BookTerminalBookPrototype();
			
			//proto.Name = Name(item.Value) ?? "";
			//proto.Description = Description(item.Value) ?? "";
			//proto.Content = paperComp.Content ?? "";
			//proto.StampState = paperComp.StampState ?? "paper_stamp-void";
			//
			//foreach (var entry in paperComp.StampedBy)
			//{
			//	var reshapedEntry = new List<string>();
			//	reshapedEntry.Add(entry.StampedName);
			//	reshapedEntry.Add(entry.StampedColor.ToHex());
			//	proto.StampedBy.Add(reshapedEntry);
			//}
			
			RefreshBookContent();
			
			paperComp.Content = (bookTerminalEntries.Count() > 0) ? $"111 { bookTerminalEntries[0].Id }" : "000 _";
		}
		
		private void ClickSound(Entity<BookTerminalComponent> reagentDispenser)
        {
            _audioSystem.PlayPvs(reagentDispenser.Comp.ClickSound, reagentDispenser, AudioParams.Default.WithVolume(-2f));
        }
    }
}


