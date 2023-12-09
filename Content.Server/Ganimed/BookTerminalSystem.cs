using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Database;
using Content.Server.Power.EntitySystems;
using Content.Server.Power.Components;
using Content.Server.Ganimed.Components;
using Content.Shared.Examine;
using Content.Server.UserInterface;
using Content.Shared.Ganimed;
using JetBrains.Annotations;
using Content.Server.Popups;
using Content.Shared.Audio;
using Content.Shared.Containers.ItemSlots;
using Content.Server.Paper;
using Content.Shared.Paper;
using Content.Shared.Tag;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Server.Labels.Components;
using System.Threading.Tasks;
using Content.Shared.Ganimed;
using Content.Shared.Ganimed.Components;
using Content.Shared.Emag.Components;
using Content.Shared.Construction.Components;
using Content.Server.Construction;
using Robust.Shared.Asynchronous;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.GameObjects;
using Robust.Server.GameObjects;

namespace Content.Server.Ganimed
{
    [UsedImplicitly]
	public sealed class BookTerminalSystem : EntitySystem
    {
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedAmbientSoundSystem _ambientSoundSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;
        [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
        [Dependency] private readonly AccessReaderSystem _accessReader = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
		[Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly MetaDataSystem _metaData = default!;
        [Dependency] private readonly PaperSystem _paperSystem = default!;
		[Dependency] private readonly IRobustRandom _random = default!;
		[Dependency] private readonly ITaskManager _task = default!;
		[Dependency] private readonly TagSystem _tag = default!;
		
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
            SubscribeLocalEvent<BookTerminalComponent, BookTerminalCopyPasteMessage>(OnCopyPasteMessage);
			SubscribeLocalEvent<BookTerminalComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<BookTerminalComponent, UpgradeExamineEvent>(OnUpgradeExamine);
            SubscribeLocalEvent<BookTerminalComponent, RefreshPartsEvent>(OnPartsRefresh);
            SubscribeLocalEvent<BookTerminalComponent, UnanchorAttemptEvent>(OnUnanchorAttempt);
			SubscribeLocalEvent<BookTerminalCartridgeComponent, ExaminedEvent>(OnExamined); // Мне лень писать под это отдельную систему.
        }
		
		public override void Update(float frameTime)
		{
			base.Update(frameTime);

			var query = EntityQueryEnumerator<BookTerminalComponent, ApcPowerReceiverComponent>();
			while (query.MoveNext(out var uid, out var terminal, out var receiver))
			{
				if (!Transform(uid).Anchored || !receiver.Powered)
				{
					FlushTask((uid, terminal));
					SetLockOnAllSlots((uid, terminal), !receiver.Powered && Transform(uid).Anchored);
					continue;
				}
				
				if (terminal.WorkType is not null && terminal.WorkTimeRemaining > 0.0f)
				{
					terminal.WorkTimeRemaining -= frameTime * terminal.TimeMultiplier;
					if (terminal.WorkTimeRemaining <= 0.0f)
						ProcessTask((uid, terminal));
				}
			}
		}
		
		private void UpdateVisuals(Entity<BookTerminalComponent> ent)
		{
			var cartridge = _itemSlotsSystem.GetItemOrNull(ent, "cartridgeSlot");
			
			var workInProgress = (ent.Comp.WorkType is not null && ent.Comp.WorkTimeRemaining > 0.0f);
			
			_appearanceSystem.SetData(ent, BookTerminalVisualLayers.Working, workInProgress);
			
			if (EntityManager.TryGetComponent<BookTerminalVisualsComponent>(ent, out BookTerminalVisualsComponent? visualsComp))
			{
				visualsComp.DoWorkAnimation = workInProgress;
				Dirty(visualsComp);
			}
			
			if (cartridge is not null && EntityManager.TryGetComponent<BookTerminalCartridgeComponent>(cartridge, out BookTerminalCartridgeComponent? cartridgeComp))
			{
				_appearanceSystem.SetData(ent, BookTerminalVisualLayers.Slotted, true);
				_appearanceSystem.SetData(ent, BookTerminalVisualLayers.Full, cartridgeComp.CurrentCharge == cartridgeComp.FullCharge);
				_appearanceSystem.SetData(ent, BookTerminalVisualLayers.High, cartridgeComp.CurrentCharge >= cartridgeComp.FullCharge / 1.43f && cartridgeComp.CurrentCharge < cartridgeComp.FullCharge);
				_appearanceSystem.SetData(ent, BookTerminalVisualLayers.Medium, cartridgeComp.CurrentCharge >= cartridgeComp.FullCharge / 2.5f && cartridgeComp.CurrentCharge < cartridgeComp.FullCharge / 1.43f);
				_appearanceSystem.SetData(ent, BookTerminalVisualLayers.Low, cartridgeComp.CurrentCharge > 0 && cartridgeComp.CurrentCharge < cartridgeComp.FullCharge / 2.5f);
				_appearanceSystem.SetData(ent, BookTerminalVisualLayers.None, cartridgeComp.CurrentCharge < 1);
				return;
			}
			
			_appearanceSystem.SetData(ent, BookTerminalVisualLayers.Slotted, true);
			_appearanceSystem.SetData(ent, BookTerminalVisualLayers.Full, false);
			_appearanceSystem.SetData(ent, BookTerminalVisualLayers.High, false);
			_appearanceSystem.SetData(ent, BookTerminalVisualLayers.Medium, false);
			_appearanceSystem.SetData(ent, BookTerminalVisualLayers.Low, false);
			_appearanceSystem.SetData(ent, BookTerminalVisualLayers.None, false);
		}
		
        private void OnPartsRefresh(EntityUid uid, BookTerminalComponent component, RefreshPartsEvent args)
        {
            component.TimeMultiplier = 1 / MathF.Pow(0.65f, args.PartRatings["Manipulator"] - 1);
            component.CartridgeUsage = component.BaseCartridgeUsage * MathF.Pow(0.75f, args.PartRatings["MatterBin"] - 1);
        }
		
		private void OnUpgradeExamine(EntityUid uid, BookTerminalComponent component, UpgradeExamineEvent args)
        {
            args.AddPercentageUpgrade("lathe-component-upgrade-speed", component.TimeMultiplier);
            args.AddPercentageUpgrade("lathe-component-upgrade-material-use", component.CartridgeUsage / component.BaseCartridgeUsage);
        }
		
		private void SubscribeUpdateUiState<T>(Entity<BookTerminalComponent> ent, ref T ev)
        {
            UpdateUiState(ent);
			RefreshBookContent();
        }
		
		private bool IsRoutineAllowed(Entity<BookTerminalComponent> bookTerminal)
		{
			var bookContainer = _itemSlotsSystem.GetItemOrNull(bookTerminal, "bookSlot");
			var cartridgeContainer = _itemSlotsSystem.GetItemOrNull(bookTerminal, "cartridgeSlot");
			
			return bookContainer is not null && 
				cartridgeContainer is not null && 
				TryComp<BookTerminalCartridgeComponent>(cartridgeContainer, out var cartridgeComp) && 
				cartridgeComp.CurrentCharge > bookTerminal.Comp.CartridgeUsage &&
				cartridgeComp.FullCharge > bookTerminal.Comp.CartridgeUsage;
		}
		
		private void DecreaseCartridgeCharge(Entity<BookTerminalComponent> bookTerminal)
		{
			var cartridgeContainer = _itemSlotsSystem.GetItemOrNull(bookTerminal, "cartridgeSlot");
			if (!TryComp<BookTerminalCartridgeComponent>(cartridgeContainer, out var cartridgeComp)
				|| cartridgeComp.CurrentCharge < bookTerminal.Comp.CartridgeUsage)
				return;
				
			cartridgeComp.CurrentCharge -= bookTerminal.Comp.CartridgeUsage;
			Dirty(cartridgeComp);
		}
		
		private bool TryLowerCartridgeCharge(Entity<BookTerminalComponent> bookTerminal)
		{
			if (!IsRoutineAllowed(bookTerminal))
			{
				_popupSystem.PopupEntity(Loc.GetString("book-terminal-cartridge-component-empty"), bookTerminal);
				return false;
			}
			
			DecreaseCartridgeCharge(bookTerminal);
			return true;
		}
		
		private void OnExamined(EntityUid uid, BookTerminalCartridgeComponent component, ExaminedEvent args)
		{
			args.PushText(Loc.GetString("book-terminal-cartridge-component-examine", ("charge", (int)(component.CurrentCharge / component.FullCharge * 100))));
		}
		
		private void OnPowerChanged(EntityUid uid, BookTerminalComponent component, ref PowerChangedEvent args)
		{
			FlushTask((uid, component));
			
			SetLockOnAllSlots((uid, component), !args.Powered);

			UpdateVisuals((uid, component));
		}
		
		private void OnUnanchorAttempt(EntityUid uid, BookTerminalComponent component, UnanchorAttemptEvent args)
        {
            if (!Transform(uid).Anchored)
				FlushTask((uid, component));
        }
		
		private void UpdateUiState(Entity<BookTerminalComponent> bookTerminal)
        {
            var bookContainer = _itemSlotsSystem.GetItemOrNull(bookTerminal, "bookSlot");
			var cartridgeContainer = _itemSlotsSystem.GetItemOrNull(bookTerminal, "cartridgeSlot");
			var bookName = bookContainer is not null ? Name(bookContainer.Value) : null;
			var bookDescription = bookContainer is not null ? Description(bookContainer.Value) : null;
			float? cartridgeCharge = cartridgeContainer is not null ?
									EntityManager.TryGetComponent<BookTerminalCartridgeComponent>(cartridgeContainer, out var cartridgeComp) ? 
									cartridgeComp.CurrentCharge / cartridgeComp.FullCharge : 
									null :
									null;
			
			float? workProgress = bookTerminal.Comp.WorkTimeRemaining > 0.0f && bookTerminal.Comp.WorkType is not null ?
									bookTerminal.Comp.WorkTimeRemaining / bookTerminal.Comp.WorkTime : null;

            var state = new BookTerminalBoundUserInterfaceState(bookName, 
							bookDescription, 
							GetNetEntity(bookContainer), 
							bookTerminalEntries, 
							IsRoutineAllowed(bookTerminal), 
							cartridgeCharge, 
							workProgress,
							bookTerminal.Comp.PrintBookEntry is not null);
            _userInterfaceSystem.TrySetUiState(bookTerminal, BookTerminalUiKey.Key, state);
			UpdateVisuals(bookTerminal);
        }
		
		private void SetLockOnAllSlots(Entity<BookTerminalComponent> bookTerminal, bool lockValue)
		{
			_itemSlotsSystem.SetLock(bookTerminal, "cartridgeSlot", lockValue);
			_itemSlotsSystem.SetLock(bookTerminal, "bookSlot", lockValue);
		}
		
		private void OnClearContainerMessage(Entity<BookTerminalComponent> bookTerminal, ref BookTerminalClearContainerMessage message)
        {
            var bookContainer = _itemSlotsSystem.GetItemOrNull(bookTerminal, "bookSlot");
            if (bookContainer is not {Valid: true})
                return;
			
			if (message.Session.AttachedEntity is not { Valid: true } entity || Deleted(entity))
                return;
			
			if (IsAuthorized(bookTerminal, entity, bookTerminal) && TryLowerCartridgeCharge(bookTerminal))
				SetupTask(bookTerminal, "Clearing");
			
			UpdateUiState(bookTerminal);
        }
		
		private void OnUploadMessage(Entity<BookTerminalComponent> bookTerminal, ref BookTerminalUploadMessage message)
        {
            var bookContainer = _itemSlotsSystem.GetItemOrNull(bookTerminal, "bookSlot");
            if (bookContainer is not {Valid: true})
                return;
			
			if (message.Session.AttachedEntity is not { Valid: true } entity || Deleted(entity))
                return;
			
			if (IsAuthorized(bookTerminal, entity, bookTerminal) && TryLowerCartridgeCharge(bookTerminal))
			{
				var content = GetContent(bookContainer.Value);
				if (content is not null)
					UploadBookContent(content); // Иначе, асинхронное обновление просто не поспевает :|
				SetupTask(bookTerminal, "Uploading");
			}
			
			UpdateUiState(bookTerminal);
        }
		
		private void OnPrintBookMessage(Entity<BookTerminalComponent> bookTerminal, ref BookTerminalPrintBookMessage message)
        {
            var bookContainer = _itemSlotsSystem.GetItemOrNull(bookTerminal, "bookSlot");
            if (bookContainer is not {Valid: true})
                return;
			if (bookTerminalEntries.Count() < 1)
				return;
			
			if (message.Session.AttachedEntity is not { Valid: true } entity || Deleted(entity))
                return;
			
			RefreshBookContent();
			
			if (IsAuthorized(bookTerminal, entity, bookTerminal) && TryLowerCartridgeCharge(bookTerminal))
			{
				bookTerminal.Comp.PrintBookEntry = message.BookEntry;
				SetupTask(bookTerminal, "Printing");
			}
			
            UpdateUiState(bookTerminal);
        }
		
		private void OnCopyPasteMessage(Entity<BookTerminalComponent> bookTerminal, ref BookTerminalCopyPasteMessage message)
        {
            var bookContainer = _itemSlotsSystem.GetItemOrNull(bookTerminal, "bookSlot");
            if (bookContainer is not {Valid: true})
                return;
			if (bookTerminalEntries.Count() < 1)
				return;
			
			if (message.Session.AttachedEntity is not { Valid: true } entity || Deleted(entity))
                return;
			
			RefreshBookContent();
			
			if (IsAuthorized(bookTerminal, entity, bookTerminal))
			{
				if (bookTerminal.Comp.PrintBookEntry is null)
				{
					bookTerminal.Comp.PrintBookEntry = GetContent(bookContainer.Value);
				} 
				else if (TryLowerCartridgeCharge(bookTerminal))
				{
					SetupTask(bookTerminal, "Printing");
				}
					
			}
			
            UpdateUiState(bookTerminal);
        }
		
		private void ProcessTask(Entity<BookTerminalComponent> bookTerminal)
		{
			var bookContainer = _itemSlotsSystem.GetItemOrNull(bookTerminal, "bookSlot");
			
			if (bookContainer is {Valid: true})
			{
				if (bookTerminal.Comp.WorkType == "Clearing" || bookTerminal.Comp.WorkType == "Printing")
					ClearContent(bookContainer.Value);
				
				if (bookTerminal.Comp.WorkType == "Printing" && bookTerminal.Comp.PrintBookEntry is not null)
					SetContent(bookContainer.Value, bookTerminal.Comp.PrintBookEntry, bookTerminal);
				
			}
			
			FlushTask(bookTerminal);
			UpdateUiState(bookTerminal);
			
            _audio.PlayPvs(bookTerminal.Comp.ClickSound, bookTerminal, AudioParams.Default.WithVolume(5f));
			
		}
		
		private void SetupTask(Entity<BookTerminalComponent> bookTerminal, string? taskName)
		{
			SetLockOnAllSlots(bookTerminal, true);
			bookTerminal.Comp.WorkTimeRemaining = bookTerminal.Comp.WorkTime;
			bookTerminal.Comp.WorkType = taskName;
			
			_audio.PlayPvs(bookTerminal.Comp.WorkSound, bookTerminal, AudioParams.Default.WithVolume(5f));
            _ambientSoundSystem.SetAmbience(bookTerminal, true);
			UpdateVisuals(bookTerminal);
		}
		
		private void FlushTask(Entity<BookTerminalComponent> bookTerminal)
		{
			RefreshBookContent();
			SetLockOnAllSlots(bookTerminal, false);
			bookTerminal.Comp.PrintBookEntry = null;
			bookTerminal.Comp.WorkType = null;
			bookTerminal.Comp.WorkTimeRemaining = 0.0f;
			
            _ambientSoundSystem.SetAmbience(bookTerminal, false);
			UpdateVisuals(bookTerminal);
			UpdateUiState(bookTerminal);
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
		
		private void SetContent(EntityUid? item, SharedBookTerminalEntry bookEntry, Entity<BookTerminalComponent> bookTerminal)
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
			
			if (!HasComp<EmaggedComponent>(bookTerminal) && bookTerminal.Comp.StampedName is not null)
			{
				var stampedColor = bookTerminal.Comp.StampedColor is not null ? 
									Color.FromHex(bookTerminal.Comp.StampedColor) : 
									Color.FromHex("#000000");
				
				var stampInfo = new StampDisplayInfo {
				BlockWriting = bookTerminal.Comp.BlockWriting,
                StampedName = bookTerminal.Comp.StampedName,
                StampedColor = stampedColor,
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
		
		private SharedBookTerminalEntry? GetContent(EntityUid? item)
		{
			if (item is null)
				return null;
			
			var paperComp = EnsureComp<PaperComponent>(item.Value);
			var metadata = EnsureComp<MetaDataComponent>(item.Value);
			
			var sharedStamps = new List<SharedStampedData>();
			
			foreach (var entry in paperComp.StampedBy)
			{
				sharedStamps.Add(new SharedStampedData(-1, entry.StampedName, entry.StampedColor.ToHex()));
			}
			
			return new SharedBookTerminalEntry(-1, 
				Name(item.Value) ?? "",  
				Description(item.Value) ?? "", 
				paperComp.Content ?? "",
				sharedStamps,
				paperComp.StampState ?? "paper_stamp-void");
		}
		
		public async void UploadBookContent(SharedBookTerminalEntry sharedBookEntry)
		{
			var db = IoCManager.Resolve<IServerDbManager>();
			
			BookTerminalEntry bookEntry = new BookTerminalEntry();
			
			List<StampedData> stampData = new List<StampedData>();
			
			foreach (var entry in sharedBookEntry.StampedBy)
			{
				var entryData = new StampedData();
				entryData.Name = entry.Name;
				entryData.Color = entry.Color;
				stampData.Add(entryData);
			}
			
			bookEntry.Name = sharedBookEntry.Name;
			bookEntry.Description = sharedBookEntry.Description;
			bookEntry.Content = sharedBookEntry.Content;
			bookEntry.StampState = sharedBookEntry.StampState;
			bookEntry.StampedBy = stampData;
			
			await db.UploadBookTerminalEntryAsync(bookEntry);
			
			RefreshBookContent();
		}
		
		public bool IsAuthorized(EntityUid uid, EntityUid user, BookTerminalComponent? bookTerminal = null)
        {
            if (!Resolve(uid, ref bookTerminal))
                return false;

            if (!TryComp<AccessReaderComponent>(uid, out var accessReader))
                return true;

            if (_accessReader.IsAllowed(user, uid, accessReader) || HasComp<EmaggedComponent>(uid))
                return true;

            _popupSystem.PopupEntity(Loc.GetString("book-terminal-component-access-denied"), uid);
            return false;
        }
    }
}


