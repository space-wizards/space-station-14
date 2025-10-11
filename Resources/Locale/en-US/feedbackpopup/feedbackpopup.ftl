feedbackpopup-window-name = Request for feedback

feedbackpopup-control-button-text = Open Link

feedbackpopup-control-total-surveys = {$num ->
    [one] {$num} entry
   *[other] {$num} entries
}
feedbackpopup-control-ui-footer = Let us know what you think!

# Command strings
command-description-showfeedbackpopup = Open the feedback popup window for the given sessions or for all sessions if none are passed.
command-description-openfeedbackpopup = Open the feedback popup window.
command-description-addfeedbackpopup = Adds a feedback popup prototype to the given clients and opens the popup window if the client didn't already have the prototype listed.

feedbackpopup-give-command-name = givefeedbackpopup
feedbackpopup-show-command-name = showfeedbackpopup
cmd-givefeedbackpopup-desc = Gives the targeted player a feedback popup.
cmd-givefeedbackpopup-help = Usage: givefeedbackpopup <playerUid> <prototypeId>
cmd-showfeedbackpopup-desc = Open the feedback popup window.
cmd-showfeedbackpopup-help = Usage: showfeedbackpopup
feedbackpopup-command-error-invalid-proto = Invalid feedback popup prototype.
feedbackpopup-command-error-popup-send-fail = Couldn't send popup. There probably isn't a mind attached to the given entity.
feedbackpopup-command-success = Sent popup!
feedbackpopup-command-hint-playerUid = <playerUid>
feedbackpopup-command-hint-protoId = <prototypeId>
