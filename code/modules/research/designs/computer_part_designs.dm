////////////////////////////////////////
///////////Computer Parts///////////////
////////////////////////////////////////

/datum/design/disk/normal
	name = "Hard Disk Drive"
	id = "hdd_basic"
	build_type = PROTOLATHE
	materials = list(/datum/material/iron = 400, /datum/material/glass = 100)
	build_path = /obj/item/computer_hardware/hard_drive
	category = list("Computer Parts")
	departmental_flags = DEPARTMENTAL_FLAG_SCIENCE | DEPARTMENTAL_FLAG_ENGINEERING

/datum/design/disk/advanced
	name = "Advanced Hard Disk Drive"
	id = "hdd_advanced"
	build_type = PROTOLATHE
	materials = list(/datum/material/iron = 800, /datum/material/glass = 200)
	build_path = /obj/item/computer_hardware/hard_drive/advanced
	category = list("Computer Parts")
	departmental_flags = DEPARTMENTAL_FLAG_SCIENCE | DEPARTMENTAL_FLAG_ENGINEERING

/datum/design/disk/super
	name = "Super Hard Disk Drive"
	id = "hdd_super"
	build_type = PROTOLATHE
	materials = list(/datum/material/iron = 1600, /datum/material/glass = 400)
	build_path = /obj/item/computer_hardware/hard_drive/super
	category = list("Computer Parts")
	departmental_flags = DEPARTMENTAL_FLAG_SCIENCE | DEPARTMENTAL_FLAG_ENGINEERING

/datum/design/disk/cluster
	name = "Cluster Hard Disk Drive"
	id = "hdd_cluster"
	build_type = PROTOLATHE
	materials = list(/datum/material/iron = 3200, /datum/material/glass = 800)
	build_path = /obj/item/computer_hardware/hard_drive/cluster
	category = list("Computer Parts")
	departmental_flags = DEPARTMENTAL_FLAG_SCIENCE | DEPARTMENTAL_FLAG_ENGINEERING

/datum/design/disk/small
	name = "Solid State Drive"
	id = "ssd_small"
	build_type = PROTOLATHE
	materials = list(/datum/material/iron = 800, /datum/material/glass = 200)
	build_path = /obj/item/computer_hardware/hard_drive/small
	category = list("Computer Parts")
	departmental_flags = DEPARTMENTAL_FLAG_SCIENCE | DEPARTMENTAL_FLAG_ENGINEERING

/datum/design/disk/micro
	name = "Micro Solid State Drive"
	id = "ssd_micro"
	build_type = PROTOLATHE
	materials = list(/datum/material/iron = 400, /datum/material/glass = 100)
	build_path = /obj/item/computer_hardware/hard_drive/micro
	category = list("Computer Parts")
	departmental_flags = DEPARTMENTAL_FLAG_SCIENCE | DEPARTMENTAL_FLAG_ENGINEERING

// Network cards
/datum/design/netcard/basic
	name = "Network Card"
	id = "netcard_basic"
	build_type = IMPRINTER
	materials = list(/datum/material/iron = 250, /datum/material/glass = 100)
	build_path = /obj/item/computer_hardware/network_card
	category = list("Computer Parts")
	departmental_flags = DEPARTMENTAL_FLAG_SCIENCE | DEPARTMENTAL_FLAG_ENGINEERING

/datum/design/netcard/advanced
	name = "Advanced Network Card"
	id = "netcard_advanced"
	build_type = IMPRINTER
	materials = list(/datum/material/iron = 500, /datum/material/glass = 200)
	build_path = /obj/item/computer_hardware/network_card/advanced
	category = list("Computer Parts")
	departmental_flags = DEPARTMENTAL_FLAG_SCIENCE | DEPARTMENTAL_FLAG_ENGINEERING

/datum/design/netcard/wired
	name = "Wired Network Card"
	id = "netcard_wired"
	build_type = IMPRINTER
	materials = list(/datum/material/iron = 2500, /datum/material/glass = 400)
	build_path = /obj/item/computer_hardware/network_card/wired
	category = list("Computer Parts")
	departmental_flags = DEPARTMENTAL_FLAG_SCIENCE | DEPARTMENTAL_FLAG_ENGINEERING

// Data disks
/datum/design/portabledrive/basic
	name = "Data Disk"
	id = "portadrive_basic"
	build_type = IMPRINTER
	materials = list(/datum/material/glass = 800)
	build_path = /obj/item/computer_hardware/hard_drive/portable
	category = list("Computer Parts")
	departmental_flags = DEPARTMENTAL_FLAG_SCIENCE | DEPARTMENTAL_FLAG_ENGINEERING

/datum/design/portabledrive/advanced
	name = "Advanced Data Disk"
	id = "portadrive_advanced"
	build_type = IMPRINTER
	materials = list(/datum/material/glass = 1600)
	build_path = /obj/item/computer_hardware/hard_drive/portable/advanced
	category = list("Computer Parts")
	departmental_flags = DEPARTMENTAL_FLAG_SCIENCE | DEPARTMENTAL_FLAG_ENGINEERING

/datum/design/portabledrive/super
	name = "Super Data Disk"
	id = "portadrive_super"
	build_type = IMPRINTER
	materials = list(/datum/material/glass = 3200)
	build_path = /obj/item/computer_hardware/hard_drive/portable/super
	category = list("Computer Parts")
	departmental_flags = DEPARTMENTAL_FLAG_SCIENCE | DEPARTMENTAL_FLAG_ENGINEERING

// Card slot
/datum/design/cardslot
	name = "ID Card Slot"
	id = "cardslot"
	build_type = PROTOLATHE
	materials = list(/datum/material/iron = 600)
	build_path = /obj/item/computer_hardware/card_slot
	category = list("Computer Parts")
	departmental_flags = DEPARTMENTAL_FLAG_SCIENCE | DEPARTMENTAL_FLAG_ENGINEERING

// Intellicard slot
/datum/design/aislot
	name = "Intellicard Slot"
	id = "aislot"
	build_type = PROTOLATHE
	materials = list(/datum/material/iron = 600)
	build_path = /obj/item/computer_hardware/ai_slot
	category = list("Computer Parts")
	departmental_flags = DEPARTMENTAL_FLAG_SCIENCE | DEPARTMENTAL_FLAG_ENGINEERING

// Mini printer
/datum/design/miniprinter
	name = "Miniprinter"
	id = "miniprinter"
	build_type = PROTOLATHE
	materials = list(/datum/material/iron = 600)
	build_path = /obj/item/computer_hardware/printer/mini
	category = list("Computer Parts")
	departmental_flags = DEPARTMENTAL_FLAG_SCIENCE | DEPARTMENTAL_FLAG_ENGINEERING

// APC Link
/datum/design/APClink
	name = "Area Power Connector"
	id = "APClink"
	build_type = PROTOLATHE
	materials = list(/datum/material/iron = 2000)
	build_path = /obj/item/computer_hardware/recharger/APC
	category = list("Computer Parts")
	departmental_flags = DEPARTMENTAL_FLAG_SCIENCE | DEPARTMENTAL_FLAG_ENGINEERING

// Batteries
/datum/design/battery/controller
	name = "Power Cell Controller"
	id = "bat_control"
	build_type = PROTOLATHE
	materials = list(/datum/material/iron = 400)
	build_path = /obj/item/computer_hardware/battery
	category = list("Computer Parts")
	departmental_flags = DEPARTMENTAL_FLAG_SCIENCE | DEPARTMENTAL_FLAG_ENGINEERING

/datum/design/battery/normal
	name = "Battery Module"
	id = "bat_normal"
	build_type = PROTOLATHE
	materials = list(/datum/material/iron = 400)
	build_path = /obj/item/stock_parts/cell/computer
	category = list("Computer Parts")
	departmental_flags = DEPARTMENTAL_FLAG_SCIENCE | DEPARTMENTAL_FLAG_ENGINEERING

/datum/design/battery/advanced
	name = "Advanced Battery Module"
	id = "bat_advanced"
	build_type = PROTOLATHE
	materials = list(/datum/material/iron = 800)
	build_path = /obj/item/stock_parts/cell/computer/advanced
	category = list("Computer Parts")
	departmental_flags = DEPARTMENTAL_FLAG_SCIENCE | DEPARTMENTAL_FLAG_ENGINEERING

/datum/design/battery/super
	name = "Super Battery Module"
	id = "bat_super"
	build_type = PROTOLATHE
	materials = list(/datum/material/iron = 1600)
	build_path = /obj/item/stock_parts/cell/computer/super
	category = list("Computer Parts")
	departmental_flags = DEPARTMENTAL_FLAG_SCIENCE | DEPARTMENTAL_FLAG_ENGINEERING

/datum/design/battery/nano
	name = "Nano Battery Module"
	id = "bat_nano"
	build_type = PROTOLATHE
	materials = list(/datum/material/iron = 200)
	build_path = /obj/item/stock_parts/cell/computer/nano
	category = list("Computer Parts")
	departmental_flags = DEPARTMENTAL_FLAG_SCIENCE | DEPARTMENTAL_FLAG_ENGINEERING

/datum/design/battery/micro
	name = "Micro Battery Module"
	id = "bat_micro"
	build_type = PROTOLATHE
	materials = list(/datum/material/iron = 400)
	build_path = /obj/item/stock_parts/cell/computer/micro
	category = list("Computer Parts")
	departmental_flags = DEPARTMENTAL_FLAG_SCIENCE | DEPARTMENTAL_FLAG_ENGINEERING

// Processor unit
/datum/design/cpu
	name = "Processor Board"
	id = "cpu_normal"
	build_type = IMPRINTER
	materials = list(/datum/material/glass = 1600)
	build_path = /obj/item/computer_hardware/processor_unit
	category = list("Computer Parts")
	departmental_flags = DEPARTMENTAL_FLAG_SCIENCE | DEPARTMENTAL_FLAG_ENGINEERING

/datum/design/cpu/small
	name = "Microprocessor"
	id = "cpu_small"
	build_type = IMPRINTER
	materials = list(/datum/material/glass = 800)
	build_path = /obj/item/computer_hardware/processor_unit/small
	category = list("Computer Parts")
	departmental_flags = DEPARTMENTAL_FLAG_SCIENCE | DEPARTMENTAL_FLAG_ENGINEERING

/datum/design/cpu/photonic
	name = "Photonic Processor Board"
	id = "pcpu_normal"
	build_type = IMPRINTER
	materials = list(/datum/material/glass = 6400, /datum/material/gold = 2000)
	build_path = /obj/item/computer_hardware/processor_unit/photonic
	category = list("Computer Parts")
	departmental_flags = DEPARTMENTAL_FLAG_SCIENCE | DEPARTMENTAL_FLAG_ENGINEERING

/datum/design/cpu/photonic/small
	name = "Photonic Microprocessor"
	id = "pcpu_small"
	build_type = IMPRINTER
	materials = list(/datum/material/glass = 3200, /datum/material/gold = 1000)
	build_path = /obj/item/computer_hardware/processor_unit/photonic/small
	category = list("Computer Parts")
	departmental_flags = DEPARTMENTAL_FLAG_SCIENCE | DEPARTMENTAL_FLAG_ENGINEERING
