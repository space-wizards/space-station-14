# Shown when greeted with the Suspicion role
suspicion-role-greeting = Вы { $roleName }!
# Shown when greeted with the Suspicion role
suspicion-objective = Цель: { $objectiveText }
# Shown when greeted with the Suspicion role
suspicion-partners-in-crime =
    { $partnersCount ->
        [zero] Вы сами по себе. Удачи!
        [one] Ваш союзник: { $partnerNames }.
       *[other] Ваши союзники: { $partnerNames }.
    }
