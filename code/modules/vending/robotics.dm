//This one's from bay12
/obj/machinery/vending/robotics
	name = "\improper Robotech Deluxe"
	desc = "All the tools you need to create your own robot army."
	icon_state = "robotics"
	icon_deny = "robotics-deny"
	req_access = list(ACCESS_ROBOTICS)
	products = list(/obj/item/clothing/suit/toggle/labcoat = 4,
					/obj/item/clothing/under/rank/rnd/roboticist = 4,
					/obj/item/stack/cable_coil = 4,
					/obj/item/assembly/flash/handheld = 4,
					/obj/item/stock_parts/cell/high = 12,
					/obj/item/assembly/prox_sensor = 3,
					/obj/item/assembly/signaler = 3,
					/obj/item/healthanalyzer = 3,
					/obj/item/scalpel = 2,
					/obj/item/circular_saw = 2,
					/obj/item/tank/internals/anesthetic = 2,
					/obj/item/clothing/mask/breath/medical = 5,
					/obj/item/screwdriver = 5,
					/obj/item/crowbar = 5)
	refill_canister = /obj/item/vending_refill/robotics
	default_price = 600
	payment_department = ACCOUNT_SCI

/obj/item/vending_refill/robotics
	machine_name = "Robotech Deluxe"
	icon_state = "refill_engi"
