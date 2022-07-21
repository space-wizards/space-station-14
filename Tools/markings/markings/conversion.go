package markings

func accessories_to_markings(accessories []SpriteAccessoryPrototype) ([]MarkingPrototype, error) {
	res := make([]MarkingPrototype, 0)

	for _, v := range accessories {
		marking, err := v.toMarking()
		if err != nil {
			return nil, err
		}

		res = append(res, *marking)
	}

	return res, nil
}
