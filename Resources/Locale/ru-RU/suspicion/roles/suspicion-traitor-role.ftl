# Shown when greeted with the Suspicion role
suspicion-role-greeting = Вы { $roleName }!
# Shown when greeted with the Suspicion role
suspicion-objective = Цель: { $objectiveText }
# Shown when greeted with the Suspicion role
suspicion-partners-in-crime =
    { $partnersCount ->
        [zero] You're on your own. Good luck!
        [one] Your partner in crime is { $partnerNames }.
       *[other] Your partners in crime are { $partnerNames }.
    }
