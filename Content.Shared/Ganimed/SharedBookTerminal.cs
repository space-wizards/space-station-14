using Robust.Shared.Serialization;

namespace Content.Shared.Ganimed
{

    [Serializable, NetSerializable]
    public sealed class BookTerminalBoundUserInterfaceState : BoundUserInterfaceState
    {
        public readonly string? BookName;
		public readonly string? BookDescription;
		public readonly NetEntity? BookEntity;
		public readonly List<SharedBookTerminalEntry>? BookEntries;
		public readonly bool RoutineAllowed;
		public readonly int? CartridgeCharge;
        
		public BookTerminalBoundUserInterfaceState(string? bookName, string? bookDescription, NetEntity? bookEntity, List<SharedBookTerminalEntry>? bookEntries, bool routineAllowed = false, int? cartridgeCharge = null)
        {
            BookName = bookName;
			BookDescription = bookDescription;
			BookEntity = bookEntity;
			BookEntries = bookEntries;
			RoutineAllowed = routineAllowed;
			CartridgeCharge = cartridgeCharge;
        }
    }
	
	[Serializable, NetSerializable]
    public sealed class BookTerminalPrintBookMessage : BoundUserInterfaceMessage
    {
        public readonly SharedBookTerminalEntry BookEntry;

        public BookTerminalPrintBookMessage(SharedBookTerminalEntry bookEntry)
        {
            BookEntry = bookEntry;
        }
    }
	
	[Serializable, NetSerializable]
    public sealed class BookTerminalClearContainerMessage : BoundUserInterfaceMessage
    {

    }
	
	[Serializable, NetSerializable]
    public sealed class BookTerminalUploadMessage : BoundUserInterfaceMessage
    {

    }
	
	[Serializable, NetSerializable]
    public class SharedBookTerminalEntry
    {
		public int Id { get; set; } = default!;
		public string Name { get; set; } = default!;
		public string Description { get; set; } = default!;
		public string Content { get; set; } = default!;
		public List<SharedStampedData> StampedBy { get; set; } = default!;
		public string StampState { get; set; } = default!;
        
		public SharedBookTerminalEntry(int id, string name, string description, string content, List<SharedStampedData> stampedBy, string stampState)
        {
            Id = id;
			Name = name;
			Description = description;
			Content = content;
			StampedBy = stampedBy;
			StampState = stampState;
        }
    }
	
	[Serializable, NetSerializable]
	public class SharedStampedData
    {
        public int Id { get; set; } = default!;
		public string Name { get; set; } = default!;
		public string Color { get; set; } = default!;
        
		public SharedStampedData(int id, string name, string color)
        {
            Id = id;
			Name = name;
			Color = color;
        }
    }

    [Serializable, NetSerializable]
    public enum BookTerminalUiKey
    {
        Key
    }
}
