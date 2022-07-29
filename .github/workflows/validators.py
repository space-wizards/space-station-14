from yamale.validators import Validator

class URL(Validator):
    """ Custom URL validator for testing """
    tag = 'url'

    def _is_valid(self, value):
        return value.startswith("http")