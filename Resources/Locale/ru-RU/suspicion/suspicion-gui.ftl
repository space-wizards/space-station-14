## SuspicionGui.xaml.cs

# Shown when clicking your Role Button in Suspicion
suspicion-ally-count-display =
    { $allyCount ->
       *[zero] Вы сами по себе. Удачи!
        [one] Ваш союзник: { $allyNames }.
        [other] Ваши союзники: { $allyNames }.
    }
