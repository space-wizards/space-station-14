## SuspicionGui.xaml.cs

# Shown when clicking your Role Button in Suspicion
suspicion-ally-count-display = {$allyCount ->
    *[zero] Você não tem aliados
    [one] Seu aliado é {$allyNames}
    [other] Seus aliados são {$allyNames}
}