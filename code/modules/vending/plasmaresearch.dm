//This one's from bay12
/obj/machinery/vending/plasmaresearch
	name = "\improper Toximate 3000"
	desc = "All the fine parts you need in one vending machine!"
	products = list(/obj/item/clothing/under/rank/rnd/scientist = 6,
		            /obj/item/clothing/suit/bio_suit = 6,
		            /obj/item/clothing/head/bio_hood = 6,
					/obj/item/transfer_valve = 6,
					/obj/item/assembly/timer = 6,
					/obj/item/assembly/signaler = 6,
					/obj/item/assembly/prox_sensor = 6,
					/obj/item/assembly/igniter = 6)
	contraband = list(/obj/item/assembly/health = 3)
	default_price = 400
	extra_price = 600
	payment_department = ACCOUNT_SCI
