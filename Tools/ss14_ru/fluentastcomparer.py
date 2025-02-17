from fluent.syntax import ast
from fluentast import FluentAstAbstract
from pydash import py_


class FluentAstComparer:
    def __init__(self, sourse_parsed: ast.Resource, target_parsed: ast.Resource):
        self.sourse_parsed = sourse_parsed
        self.target_parsed = target_parsed
        self.source_elements = list(
            filter(lambda el: el, list(map(lambda e: FluentAstAbstract.create_element(e), sourse_parsed.body))))
        self.target_elements = list(
            filter(lambda el: el, list(map(lambda e: FluentAstAbstract.create_element(e), target_parsed.body))))

    # Возвращает полностью эквивалентные сообщения (не считая span)
    def get_equal_elements(self):
        comparator = lambda a, b: a.element.equals(b.element, ignored_fields=['span'])

        return py_.intersection_with(self.source_elements, self.target_elements, comparator=comparator)

    # Возвращает полностью неэквивалентные сообщения (не считая span)
    def get_not_equal_elements(self):
        comparator = lambda a, b: a.element.equals(b.element, ignored_fields=['span'])
        diff = py_.difference_with(self.source_elements, self.target_elements, comparator=comparator)

        return diff

    # Возвращает сообщения с эквивалентными именами ключей
    def get_equal_id_names(self):
        comparator = lambda a, b: a.element.equals(b.element, ignored_fields=['span', 'value', 'comment', 'attributes'])
        eq = py_.intersection_with(self.source_elements, self.target_elements, comparator=comparator)

        return eq

    # Возвращает сообщения с неэквивалентными именами ключей
    def get_not_equal_id_names(self):
        comparator = lambda a, b: a.element.equals(b.element, ignored_fields=['span', 'value', 'comment', 'attributes'])
        diff = py_.difference_with(self.source_elements, self.target_elements, comparator=comparator)

        return diff

    # Возвращает сообщения target, существующие в source
    def get_exist_id_names(self, source, target):
        comparator = lambda a, b: a.element.equals(b.element, ignored_fields=['span', 'value', 'comment', 'attributes'])
        eq = py_.intersection_with(source, target, comparator=comparator)

        return eq

    # Возвращает сообщения target, существующие в source
    def get_not_exist_id_names(self):
        comparator = lambda a, b: a.element.equals(b.element, ignored_fields=['span', 'value', 'comment', 'attributes'])
        diff = py_.difference_with(self.target_elements, self.source_elements, comparator=comparator)

        return diff

    # Возвращает сообщения с эквивалентным значением и атрибутами
    def get_equal_values_with_attrs(self):
        comparator = lambda a, b: a.element.equals(b.element, ignored_fields=['span', 'id', 'comment'])
        eq = py_.intersection_with(self.target_elements, self.source_elements, comparator=comparator)

        return eq

    # Возвращает сообщения из source с неэквивалентным значением и атрибутами
    def get_not_equal_values_with_attrs(self):
        comparator = lambda a, b: a.element.equals(b.element, ignored_fields=['span', 'id', 'comment'])
        diff = py_.difference_with(self.source_elements, self.target_elements,
                                   comparator=lambda a, b: a.element.equals(b.element,
                                                                            ignored_fields=['span', 'id', 'comment']))

        return diff

    # Возвращает сообщения из source, существующие в target и source, с неэквивалентным значением и атрибутами
    def get_not_equal_exist_values_with_attrs(self):
        diff = py_.difference_with(self.source_elements, self.target_elements,
                                   comparator=lambda a, b: a.element.equals(b.element,
                                                                            ignored_fields=['span', 'id', 'comment']))
        ex = self.get_exist_id_names(self.source_elements, self.target_elements)
        exist = py_.intersection(diff, ex)

        return exist

        # Возвращает сообщения из target с неэквивалентным значением и атрибутами

    def get_target_not_equal_values_with_attrs(self):
        comparator = lambda a, b: a.element.equals(b.element, ignored_fields=['span', 'id', 'comment'])
        diff = py_.difference_with(self.source_elements, self.target_elements, comparator=comparator)

        return diff

    # Возвращает сообщения, существующие в target и source, с неэквивалентным значением и атрибутами
    def get_target_not_equal_exist_values_with_attrs(self):
        diff = py_.difference_with(self.target_elements, self.source_elements,
                                   comparator=lambda a, b: a.element.equals(b.element,
                                                                            ignored_fields=['span', 'id', 'comment']))
        exist = py_.intersection(diff, self.get_exist_id_names(self.target_elements, self.source_elements))

        return exist

    def find_message_by_id_name(self, id_name, list):
        return py_.find(list, lambda el: el.get_id_name() == id_name)
