from fluent.syntax import ast, FluentParser, FluentSerializer
from pydash import py_

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


class FluentAstAttribute:
    def __init__(self, id, value):
        self.id = id
        self.value = value


class FluentAstAttributeFactory:
    @classmethod
    def from_yaml_element(cls, element):
        attrs = []
        if element.description:
            attrs.append(FluentAstAttribute('desc', element.description))

        if element.suffix:
            attrs.append(FluentAstAttribute('suffix', element.suffix))

        if not len(attrs):
            return None

        return attrs

class FluentAstJunk:
    def __init__(self, junk: ast.Junk):
        self.junk = junk

    def get_id_name(self):
        return self.junk.content.split('=')[0].strip()


class FluentSerializedMessage:
    @classmethod
    def from_yaml_element(cls, id, value, attributes, parent_id = None):
        if not value and not id and not parent_id:
            return None

        if not attributes or not len(attributes):
            if parent_id:
                attributes = [FluentAstAttribute('desc', '{ ' + FluentSerializedMessage.get_key(parent_id) + '.desc' + ' }')]
            else:
                return None

        message = f'{cls.get_key(id)} = {cls.get_value(value, parent_id)}\n'

        if attributes and len(attributes):
            full_message = message

            for attr in attributes:
                full_message = cls.add_attr(full_message, attr.id, attr.value)

            desc_attr = py_.find(attributes, lambda a: a.id == 'desc')
            if not desc_attr and parent_id:
                full_message = cls.add_attr(full_message, 'desc', '{ ' + FluentSerializedMessage.get_key(parent_id) + '.desc' + ' }')

            return full_message

        return cls.to_serialized_message(message)

    @staticmethod
    def to_serialized_message(string_message):
        if not string_message:
            return None

        ast_message = FluentParser().parse(string_message)
        serialized = FluentSerializer(with_junk=True).serialize(ast_message)

        return serialized if serialized else ''

    @staticmethod
    def add_attr(message_str, attr_key, attr_value):
        return f'{message_str}\n  .{attr_key} = {attr_value}'

    @staticmethod
    def get_value(value, parent_id):
        if value:
            return value
        elif parent_id:
            return '{ ' + FluentSerializedMessage.get_key(parent_id) + ' }'
        else:
            return '{ "" }'

    @staticmethod
    def get_key(id):
        return f'ent-{id}'

