# Refill Messages
comp-entity-provider-insert-entity = You insert {THE($entity)} into {THE($provider)}.
comp-entity-provider-refill-from-storage = You refill {THE($refillTarget)}.
comp-entity-provider-cannot-receive = {CAPITALIZE(THE($refillTarget))} cannot be refilled!
comp-entity-provider-cannot-transfer = {CAPITALIZE(THE($provider))} cannot be used to refill!

# Ejection Messages
comp-entity-provider-no-ejected = There's nothing to eject!
comp-entity-provider-ejected = You ejected { $amount ->
                                                [1] {INDEFINITE($entity)} {$entity}.
                                                *[other] {$amount} {MAKEPLURAL($entity)}.
                                            }
