
### UI

# Shown when clicking your Role Button in Suspicion
suspicion-ally-count-display = {$allyCount ->
    *[zero] You have no allies
    [one] Your ally is {$allyNames}
    [other] Your allies are {$allyNames}
}

# Shown when greeted with the Suspicion role
suspicion-role-greeting = You're a {$roleName}!

# Shown when greeted with the Suspicion role
suspicion-objective = Objective: {$objectiveText}

# Shown when greeted with the Suspicion role
suspicion-partners-in-crime = {$partnersCount ->
    *[zero] You're on your own. Good luck!
    [one] Your partner in crime is {$partnerNames}.
    [other] Your partners in crime are {$partnerNames}.
}