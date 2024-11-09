from yamale.validators import Validator
import validators

class License(Validator):
    tag = "license"
    licenses = [
        "CC-BY-3.0",
        "CC-BY-4.0",
        "CC-BY-SA-3.0",
        "CC-BY-SA-4.0",
        "CC-BY-NC-3.0",
        "CC-BY-NC-4.0",
        "CC-BY-NC-SA-3.0",
        "CC-BY-NC-SA-4.0",
        "CC0-1.0",
        "MIT",
        "Custom" # implies that the license is described in the copyright field.
        ]

    def _is_valid(self, value):
        return value in self.licenses

class Url(Validator):
    tag = "url"

    def _is_valid(self, value):
        # Source field is required to ensure its not neglected, but there may be no applicable URL
        return (value == "NA") or validators.url(value)