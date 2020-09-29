using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Storage.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Storage.Internal.Mapping;

namespace Content.Server.Database
{
    // Taken from https://github.com/npgsql/efcore.pg/issues/1158
    // To support inet -> (IPAddress, int) mapping.
    #pragma warning disable EF1001
    public class CustomNpgsqlTypeMappingSource : NpgsqlTypeMappingSource
    #pragma warning restore EF1001
    {
        public CustomNpgsqlTypeMappingSource(
            TypeMappingSourceDependencies dependencies,
            RelationalTypeMappingSourceDependencies relationalDependencies,
            ISqlGenerationHelper sqlGenerationHelper,
            INpgsqlOptions? npgsqlOptions = null)
            : base(dependencies, relationalDependencies, sqlGenerationHelper, npgsqlOptions)
        {
            StoreTypeMappings["inet"] =
                new RelationalTypeMapping[]
                {
                    new NpgsqlInetWithMaskTypeMapping(),
                    new NpgsqlInetTypeMapping()
                };
        }
    }

    // Basically copied from NpgsqlCidrTypeMapping
    public class NpgsqlInetWithMaskTypeMapping : NpgsqlTypeMapping
    {
        public NpgsqlInetWithMaskTypeMapping() : base("inet", typeof((IPAddress, int)), NpgsqlTypes.NpgsqlDbType.Inet)
        {
        }

        protected NpgsqlInetWithMaskTypeMapping(RelationalTypeMappingParameters parameters)
            : base(parameters, NpgsqlTypes.NpgsqlDbType.Inet)
        {
        }

        protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
            => new NpgsqlInetWithMaskTypeMapping(parameters);

        protected override string GenerateNonNullSqlLiteral(object value)
        {
            var cidr = ((IPAddress Address, int Subnet)) value;
            return $"INET '{cidr.Address}/{cidr.Subnet}'";
        }

        public override Expression GenerateCodeLiteral(object value)
        {
            var cidr = ((IPAddress Address, int Subnet)) value;
            return Expression.New(
                Constructor,
                Expression.Call(ParseMethod, Expression.Constant(cidr.Address.ToString())),
                Expression.Constant(cidr.Subnet));
        }

        static readonly MethodInfo ParseMethod = typeof(IPAddress).GetMethod("Parse", new[] {typeof(string)})!;

        static readonly ConstructorInfo Constructor =
            typeof((IPAddress, int)).GetConstructor(new[] {typeof(IPAddress), typeof(int)})!;
    }
}
