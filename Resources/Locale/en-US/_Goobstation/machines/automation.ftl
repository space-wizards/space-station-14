# Robotic Arm

signal-port-name-input-machine = Item: Input Machine
signal-port-description-input-machine = A machine automation slot to take items out of, instead of taking them from the floor.

signal-port-name-output-machine = Item: Output Machine
signal-port-description-output-machine = A machine automation slot to insert items into, instead of placing them on the floor.

signal-port-name-item-moved = Item Moved
signal-port-description-item-moved = Signal port that gets pulsed after an item is moved by this arm.

signal-port-name-automation-slot-filter = Item: Filter Slot
signal-port-description-automation-slot-filter = An automation slot for an automation machine's filter.

# Reagent Grinder

signal-port-name-automation-slot-beaker = Item: Beaker Slot
signal-port-description-automation-slot-beaker = An automation slot for a liquid-handling machine's beaker.

signal-port-name-automation-slot-input = Item: Input items
signal-port-description-automation-slot-input = An automation slot for a machine's input item storage.

# Flatpacker

signal-port-name-automation-slot-board = Item: Board Slot
signal-port-description-automation-slot-board = An automation slot for a flatpacker's circuitboard.

signal-port-name-automation-slot-materials = Item: Material Storage
signal-port-description-automation-slot-materials = An automation slot for inserting materials into a machine's storage.

# Disposal Unit

signal-port-name-flush = Flush
signal-port-description-flush = Signal port to toggle a disposal unit's flush mechanism.

signal-port-name-eject = Eject
signal-port-description-eject = Signal port to eject a disposal unit's contents.

signal-port-name-ready = Ready
signal-port-description-ready = Signal port that gets pulsed after a disposal unit becomes fully pressurized.

# Storage Bin

signal-port-name-automation-slot-storage = Item: Storage
signal-port-description-automation-slot-storage = An automation slot for a storage bin's inventory.

signal-port-name-storage-inserted = Inserted
signal-port-description-storage-inserted = Signal port that gets pulsed after an item is inserted into a storage bin.

signal-port-name-storage-removed = Removed
signal-port-description-storage-removed = Signal port that gets pulsed after an item is removed from a storage bin.

# Fax Machine

signal-port-name-automation-slot-paper = Item: Paper
signal-port-description-automation-slot-paper = An automation slot for a fax machine's paper tray.

signal-port-name-fax-copy = Copy Fax
signal-port-description-fax-copy = Signal port to copy a fax machine's paper.

# Constructor / Interactor

signal-port-name-machine-start = Start
signal-port-description-machine-start = Signal port to start a machine once.

signal-port-name-machine-autostart = Auto Start
signal-port-description-machine-autostart = Signal port to control starting after completing automatically.

signal-port-name-machine-started = Started
signal-port-description-machine-started = Signal port that gets pulsed after a machine starts.

signal-port-name-machine-completed = Completed
signal-port-description-machine-completed = Signal port that gets pulsed after a machine completes its work.

signal-port-name-machine-failed = Failed
signal-port-description-machine-failed = Signal port that gets pulsed after a machine fails to start.

# Interactor

signal-port-name-automation-slot-tool = Item: Tool
signal-port-description-automation-slot-tool = An automation slot for an interactor's held tool.

# Autodoc

signal-port-name-automation-slot-autodoc-hand = Item: Autodoc Hand
signal-port-description-automation-slot-autodoc-hand = An automation slot for an autodoc's held organ/part/etc from STORE ITEM / GRAB ITEM instructions.

# Gas Canister

signal-port-name-automation-slot-gas-tank = Item: Gas Tank
signal-port-description-automation-slot-gas-tank = An automation slot for a gas tank.

# Radiation Collector

signal-port-name-rad-empty = Empty
signal-port-description-rad-empty = Signal port set to HIGH if the tank is missing or below 33% pressure, LOW otherwise.

signal-port-name-rad-low = Low
signal-port-description-rad-low = Signal port set to HIGH if the tank is below 66% pressure, LOW otherwise.

signal-port-name-rad-full = Full
signal-port-description-rad-full = Signal port set to HIGH if the tank is above 66% pressure, LOW otherwise.
