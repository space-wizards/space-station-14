# Surgeon
surgery-prepare-cancel-surgeon-popup = You remove {$item} from {$target}'s {$zone}
surgery-prepare-cancel-no-zone-surgeon-popup = You remove {$item} from {$target}
surgery-prepare-cancel-self-surgeon-popup = You remove {$item} from yourself
surgery-prepare-cancel-self-no-zone-surgeon-popup = You remove {$item} from yourself

# Target
surgery-prepare-cancel-target-popup = {$user} removes {$item} from your {$zone}
surgery-prepare-startcancel-no-zone-target-popup = {$user} removes {$item} from you

# Outsider
surgery-prepare-cancel-outsider-popup = {$user} removes {$item} from {$target}'s {$zone}
surgery-prepare-cancel-no-zone-outsider-popup = {$user} removes {$item} from {$target}
surgery-prepare-cancel-self-outsider-popup = {$user} drapes {$item} over {GENDER($user) ->
  [male] his
  [female] her
  *[other] their
} {$part} to prepare for a {$procedure}
surgery-prepare-cancel-self-no-zone-outsider-popup = {$user} removes {$item} from {GENDER($user) ->
  [male] himself
  [female] herself
  *[other] themself
}
