/obj/machinery/paystand
	name = "unregistered pay stand"
	desc = "See title."
	icon = 'icons/obj/economy.dmi'
	icon_state = "card_scanner"
	density = TRUE
	anchored = TRUE
	var/locked = FALSE
	var/obj/item/card/id/my_card
	var/obj/item/assembly/signaler/signaler //attached signaler, let people attach signalers that get activated if the user's transaction limit is achieved.
	var/signaler_threshold = 0 //signaler threshold amount
	var/amount_deposited = 0 //keep track of the amount deposited over time so you can pay multiple times to reach the signaler threshold
	var/force_fee = 0 //replaces the "pay whatever" functionality with a set amount when non-zero.

/obj/machinery/paystand/attackby(obj/item/W, mob/user, params)
	if(istype(W, /obj/item/card/id))
		if(W == my_card)
			if(user.a_intent == INTENT_DISARM)
				var/rename_msg = stripped_input(user, "Rename the Paystand:", "Paystand Naming", name)
				if(!rename_msg || !user.canUseTopic(src, BE_CLOSE, FALSE, NO_TK))
					return
				name = rename_msg
				return
			else if(user.a_intent == INTENT_GRAB)
				var/force_fee_input = input(user,"Set the fee!","Set a fee!",0) as num|null
				if(isnull(force_fee_input) || !user.canUseTopic(src, BE_CLOSE, FALSE, NO_TK))
					return
				force_fee = force_fee_input
				return
			locked = !locked
			to_chat(user, "<span class='notice'>You [src.locked ? "lock" : "unlock"] the paystand, protecting the bolts from [anchored ? "loosening" : "tightening"].</span>")
			return
		if(!my_card)
			var/obj/item/card/id/assistant_mains_need_to_die = W
			if(!assistant_mains_need_to_die.registered_account)
				return
			var/msg = stripped_input(user, "Name of pay stand:", "Paystand Naming", "[user]'s Awesome Paystand")
			if(!msg)
				return
			name = msg
			desc = "Owned by [assistant_mains_need_to_die.registered_account.account_holder], pays directly into [user.p_their()] account."
			my_card = assistant_mains_need_to_die
			to_chat(user, "You link the stand to your account.")
			return
		var/obj/item/card/id/vbucks = W
		if(vbucks.registered_account)
			var/momsdebitcard = 0
			if(!force_fee)
				momsdebitcard = input(user, "How much would you like to deposit?", "Money Deposit") as null|num
			else
				momsdebitcard = force_fee
			if(!user.canUseTopic(src, BE_CLOSE, FALSE, NO_TK))
				return
			if(momsdebitcard < 1)
				to_chat(user, "<span class='warning'>ERROR: Invalid amount designated.</span>")
				return
			if(vbucks.registered_account.adjust_money(-momsdebitcard))
				purchase(vbucks.registered_account.account_holder, momsdebitcard)
				to_chat(user, "Thanks for purchasing! The vendor has been informed.")
				return
			else
				to_chat(user, "<span class='warning'>ERROR: Account has insufficient funds to make transaction.</span>")
				return
		else
			to_chat(user, "<span class='warning'>ERROR: No bank account assigned to identification card.</span>")
			return
	if(istype(W, /obj/item/holochip))
		var/obj/item/holochip/H = W
		var/cashmoney = input(user, "How much would you like to deposit?", "Money Deposit") as null|num
		if(H.spend(cashmoney, FALSE))
			purchase(user, cashmoney)
			to_chat(user, "Thanks for purchasing! The vendor has been informed.")
			return
		else
			to_chat(user, "<span class='warning'>ERROR: Insufficient funds to make transaction.</span>")
			return
	if(istype(W, /obj/item/stack/spacecash))
		to_chat(user, "What is this, the 2000s? We only take card here.")
		return
	if(istype(W, /obj/item/coin))
		to_chat(user, "What is this, the 1800s? We only take card here.")
		return
	if(istype(W, /obj/item/assembly/signaler))
		var/obj/item/assembly/signaler/S = W
		if(S.secured)
			to_chat(user, "<span class='warning'>The signaler needs to be in attachable mode to add it to the paystand!</span>")
			return
		if(!my_card)
			to_chat(user, "<span class='warning'>ERROR: No identification card has been assigned to this paystand yet!</span>")
			return
		if(!signaler)
			var/cash_limit = input(user, "Enter the minimum amount of cash needed to deposit before the signaler is activated.", "Signaler Activation Threshold") as null|num
			if(cash_limit < 1)
				to_chat(user, "<span class='warning'>ERROR: Invalid amount designated.</span>")
				return
			if(cash_limit)
				S.forceMove(src)
				signaler = S
				signaler_threshold = cash_limit
				to_chat(user, "You attach the signaler to the paystand.")
				desc += " A signaler appears to be attached to the scanner."
		else
			to_chat(user, "<span class='warning'>A signaler is already attached to this unit!</span>")

	if(default_deconstruction_screwdriver(user, "card_scanner", "card_scanner", W))
		return

	else if(default_pry_open(W))
		return

	else if(default_unfasten_wrench(user, W))
		return

	else if(default_deconstruction_crowbar(W))
		return
	else
		return ..()

/obj/machinery/paystand/proc/purchase(buyer, price)
	my_card.registered_account.adjust_money(price)
	my_card.registered_account.bank_card_talk("Purchase made at your vendor by [buyer] for [price] credits.")
	amount_deposited = amount_deposited + price
	if(signaler && amount_deposited >= signaler_threshold)
		signaler.activate()
		amount_deposited = 0

/obj/machinery/paystand/default_unfasten_wrench(mob/user, obj/item/I, time = 20)
	if(locked)
		to_chat(user, "<span class='warning'>The bolts on this paystand are currently covered!</span>")
		return FALSE
	. = ..()

/obj/machinery/paystand/examine(mob/user)
	. = ..()
	if(force_fee)
		. += "<span class='warning'>This paystand forces a payment of <b>[force_fee]</b> credit\s per swipe instead of a variable amount.</span>"
	if(user.get_active_held_item() == my_card)
		. += "<span class='notice'>Paystands can be edited through swiping your card with different intents. <b>Disarm</b> allows editing the name while <b>Grab</b> changes payment functionality.</span>"
