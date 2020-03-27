/obj/item/stack/arcadeticket
	name = "arcade tickets"
	desc = "Wow! With enough of these, you could buy a bike! ...Pssh, yeah right."
	singular_name = "arcade ticket"
	icon_state = "arcade-ticket"
	item_state = "tickets"
	w_class = WEIGHT_CLASS_TINY
	max_amount = 30

/obj/item/stack/arcadeticket/Initialize(mapload, new_amount, merge = TRUE)
	. = ..()
	update_icon()

/obj/item/stack/arcadeticket/update_icon_state()
	var/amount = get_amount()
	switch(amount)
		if(12 to INFINITY)
			icon_state = "arcade-ticket_4"
		if(6 to 12)
			icon_state = "arcade-ticket_3"
		if(2 to 6)
			icon_state = "arcade-ticket_2"
		else
			icon_state = "arcade-ticket"

/obj/item/stack/arcadeticket/proc/pay_tickets()
	amount -= 2
	if (amount == 0)
		qdel(src)

/obj/item/stack/arcadeticket/thirty
	amount = 30
