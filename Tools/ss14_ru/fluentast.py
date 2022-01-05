from fluent.syntax import ast

class FluentAstAbstract:
    @classmethod
    def get_id_name(cls, element):
        if isinstance(element, ast.Junk):
            return FluentAstJunk(element).get_id_name()
        elif isinstance(element, ast.Message):
            return FluentAstMessage(element).get_id_name()
        else:
            return None

class FluentAstMessage:
    def __init__(self, message: ast.Message):
        self.message = message

    def get_id_name(self):
        return self.message.id.name


class FluentAstJunk:
    def __init__(self, junk: ast.Junk):
        self.junk = junk

    def get_id_name(self):
        return self.junk.content.split('=')[0].strip()
