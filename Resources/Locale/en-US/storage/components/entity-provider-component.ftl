# Insertion Messages
comp-entity-provider-cannot-receive = {CAPITALIZE(THE($refillTarget))} cannot be refilled!
comp-entity-provider-cannot-transfer = {CAPITALIZE(THE($provider))} cannot be used to refill!
comp-entity-provider-full = {CAPITALIZE(THE($provider))} is already full!

# Ejection Messages
comp-entity-provider-no-ejected = There's nothing to eject!

# Menu Messages
comp-entity-provider-select-entity = Select {MAKEPLURAL($entity)}.
comp-entity-provider-eject-all-specified-entities = Eject all {MAKEPLURAL($entity)}.
comp-entity-provider-select-new-active = Switch selected stored object.

# Examine Description

comp-entity-provider-no-stored-entities = It's empty.
comp-entity-provider-has-entities = It contains the following:
comp-entity-provider-entity-listing = {$amount ->
    [one] [color=yellow]{$amount}[/color] [color=gray]{$name}[/color]
    *[other] [color=yellow]{$amount}[/color] [color=gray]{MAKEPLURAL($name)}[/color]
}
