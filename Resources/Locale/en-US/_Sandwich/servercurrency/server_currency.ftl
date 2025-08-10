# SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
# SPDX-FileCopyrightText: 2025 Aiden <aiden@djkraz.com>
# SPDX-FileCopyrightText: 2025 SX-7 <92227810+SX-7@users.noreply.github.com>
# SPDX-FileCopyrightText: 2025 gluesniffler <159397573+gluesniffler@users.noreply.github.com>
#
# SPDX-License-Identifier: AGPL-3.0-or-later

server-currency-name-singular = Sandwich Point
server-currency-name-plural = Sandwich Points

## Commands

server-currency-gift-command = gift
server-currency-gift-command-description = Gifts some of your balance to another player.
server-currency-gift-command-help = Usage: gift <player> <value>
server-currency-gift-command-error-1 = You can't gift yourself!
server-currency-gift-command-error-2 = You can not afford to gift this! You have a balance of {$balance}.
server-currency-gift-command-giver = You gave {$player} {$amount}.
server-currency-gift-command-reciever = {$player} gave you {$amount}.

server-currency-balance-command = balance
server-currency-balance-command-description = Returns your balance.
server-currency-balance-command-help = Usage: balance
server-currency-balance-command-return = You have {$balance}.

server-currency-add-command = balance:add
server-currency-add-command-description = Adds currency to a player's balance.
server-currency-add-command-help = Usage: balance:add <player> <value>

server-currency-remove-command = balance:rem
server-currency-remove-command-description = Removes currency from a player's balance.
server-currency-remove-command-help = Usage: balance:rem <player> <value>

server-currency-set-command = balance:set
server-currency-set-command-description = Sets a player's balance.
server-currency-set-command-help = Usage: balance:set <player> <value>

server-currency-get-command = balance:get
server-currency-get-command-description = Gets the balance of a player.
server-currency-get-command-help = Usage: balance:get <player>

server-currency-command-completion-1 = Username
server-currency-command-completion-2 = Value
server-currency-command-error-1 = Unable to find a player by that name.
server-currency-command-error-2 = Value must be an integer.
server-currency-command-return = {$player} has {$balance}.

# 65% Update

gs-balanceui-title = Store
gs-balanceui-confirm = Confirm

gs-balanceui-gift-label = Transfer:
gs-balanceui-gift-player = Player
gs-balanceui-gift-player-tooltip = Insert the name of the player you want to send the money to
gs-balanceui-gift-value = Value
gs-balanceui-gift-value-tooltip = Amount of money to transfer

gs-balanceui-shop-label = Buyable Tokens:
gs-balanceui-shop-empty = Out of stock!
gs-balanceui-shop-buy = Buy
gs-balanceui-shop-footer = ⚠ use Ahelp to claim your token. Only 1 use per day.

gs-balanceui-shop-token-label = Tokens
gs-balanceui-shop-tittle-label = Titles

gs-balanceui-shop-buy-token-antag = Buy an Antag Token - {$price} Sandwich Points
gs-balanceui-shop-buy-token-admin-abuse = Buy an Admin Abuse Token - {$price} Sandwich Points
gs-balanceui-shop-buy-token-hat = Buy an Hat Token - {$price} Sandwich Points
gs-balanceui-shop-buy-token-event = Buy an Event Token - {$price} Sandwich Points

gs-balanceui-shop-token-antag = High Tier Antag Token
gs-balanceui-shop-token-admin-abuse = Admin Abuse Token
gs-balanceui-shop-token-hat = Hat Token
gs-balanceui-shop-token-event = Event Token

gs-balanceui-shop-buy-token-antag-desc = Allows you become any antag. (Excluding Wizards)
gs-balanceui-shop-buy-token-admin-abuse-desc = Allows you to request an admin to abuse their powers against you. Admins are encouraged to go wild.
gs-balanceui-shop-buy-token-hat-desc = An admin will give you a random hat.
gs-balanceui-shop-buy-token-event-desc = An admin will spawn an event of your choice.

gs-balanceui-admin-add-label = Add (or subtract) points:
gs-balanceui-admin-add-player = Player name
gs-balanceui-admin-add-value = Value

gs-balanceui-remark-token-antag = Bought an antag token.
gs-balanceui-remark-token-admin-abuse = Bought an admin abuse token.
gs-balanceui-remark-token-hat = Bought an hat token.
gs-balanceui-remark-token-event = Bought an event spawn token.
gs-balanceui-shop-click-confirm = Click again to confirm
gs-balanceui-shop-purchased = Purchased {$item}
