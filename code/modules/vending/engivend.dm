/obj/machinery/vending/engivend
	name = "\improper Engi-Vend"
	desc = "Spare tool vending. What? Did you expect some witty description?"
	icon_state = "engivend"
	icon_deny = "engivend-deny"
	req_access = list(ACCESS_ENGINE_EQUIP)
	products = list(/obj/item/clothing/glasses/meson/engine = 2,
					/obj/item/clothing/glasses/welding = 3,
					/obj/item/multitool = 4,
					/obj/item/grenade/chem_grenade/smart_metal_foam = 10,
					/obj/item/geiger_counter = 5,
					/obj/item/stock_parts/cell/high = 10,
					/obj/item/electronics/airlock = 10,
					/obj/item/electronics/apc = 10,
					/obj/item/electronics/airalarm = 10,
					/obj/item/electronics/firealarm = 10,
					/obj/item/electronics/firelock = 10)
	contraband = list(/obj/item/stock_parts/cell/potato = 3)
	premium = list(/obj/item/storage/belt/utility = 3,
					/obj/item/construction/rcd/loaded = 2,
					/obj/item/storage/box/smart_metal_foam = 1)
	refill_canister = /obj/item/vending_refill/engivend
	default_price = 450
	extra_price = 500
	payment_department = ACCOUNT_ENG

/obj/item/vending_refill/engivend
	machine_name = "Engi-Vend"
	icon_state = "refill_engi"
