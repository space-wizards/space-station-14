feedbackpopup-window-name = Request for feedback

feedbackpopup-control-button-text = Open Link

feedbackpopup-control-total-surveys = {$num ->
    [one] {$num} entry
   *[other] {$num} entries
}
feedbackpopup-control-no-entries= No entries
feedbackpopup-control-ui-footer = Let us know what you think!

# Command strings
command-description-openfeedbackpopup = Opens the feedback popup window.
command-description-feedback-show = Opens the feedback popup window for the given sessions.
command-description-feedback-add = Adds a feedback popup prototype to the given clients and opens the popup window if the client didn't already have the prototype listed.
command-description-feedback-remove = Removes a feedback popup prototype from the given clients.

feedbackpopup-give-command-name = givefeedbackpopup
feedbackpopup-show-command-name = showfeedbackpopup
cmd-givefeedbackpopup-desc = Gives the targeted player a feedback popup.
cmd-givefeedbackpopup-help = Usage: givefeedbackpopup <playerUid> <prototypeId>
cmd-showfeedbackpopup-desc = Open the feedback popup window.
cmd-showfeedbackpopup-help = Usage: showfeedbackpopup
feedbackpopup-command-error-invalid-proto = Invalid feedback popup prototype.
feedbackpopup-command-error-popup-send-fail = Failed to send popup! There probably isn't a mind attached to the given entity.
feedbackpopup-command-success = Sent popup!
feedbackpopup-command-hint-playerUid = <playerUid>
feedbackpopup-command-hint-protoId = <prototypeId>
