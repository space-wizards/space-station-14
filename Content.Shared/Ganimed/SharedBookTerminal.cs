using Robust.Shared.Serialization;

namespace Content.Shared.Ganimed
{

    [Serializable, NetSerializable]
    public sealed class BookTerminalBoundUserInterfaceState : BoundUserInterfaceState
    {
        public readonly string? BookName;
		public readonly string? BookDescription;
		public readonly NetEntity? BookEntity;
        
		public BookTerminalBoundUserInterfaceState(string? bookName, string? bookDescription, NetEntity? bookEntity)
        {
            BookName = bookName;
			BookDescription = bookDescription;
			BookEntity = bookEntity;
        }
    }
	
	[Serializable, NetSerializable]
    public sealed class BookTerminalPrintBookMessage : BoundUserInterfaceMessage
    {
        public readonly BookTerminalBookPrototype BookPrototype;

        public BookTerminalPrintBookMessage(BookTerminalBookPrototype bookPrototype)
        {
            BookPrototype = bookPrototype;
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
    public enum BookTerminalUiKey
    {
        Key
    }
}
