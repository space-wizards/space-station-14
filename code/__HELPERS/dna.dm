//////////////////////////////////////////////////////////
//A bunch of helpers to make genetics less of a headache//
//////////////////////////////////////////////////////////

#define GET_INITIALIZED_MUTATION(A) GLOB.all_mutations[A]
#define GET_GENE_STRING(A, B) (B.mutation_index[A])
#define GET_SEQUENCE(A) (GLOB.full_sequences[A])
#define GET_MUTATION_TYPE_FROM_ALIAS(A) (GLOB.alias_mutations[A])

#define GET_MUTATION_STABILIZER(A) ((A.stabilizer_coeff < 0) ? 1 : A.stabilizer_coeff)
#define GET_MUTATION_SYNCHRONIZER(A) ((A.synchronizer_coeff < 0) ? 1 : A.synchronizer_coeff)
#define GET_MUTATION_POWER(A) ((A.power_coeff < 0) ? 1 : A.power_coeff)
#define GET_MUTATION_ENERGY(A) ((A.energy_coeff < 0) ? 1 : A.energy_coeff)
