from yamale.validators import Validator
import validators

class URL(Validator):
    """ Custom URL validator for testing """
    tag = 'url'

    def _is_valid(self, value):
        return validators.url(value)