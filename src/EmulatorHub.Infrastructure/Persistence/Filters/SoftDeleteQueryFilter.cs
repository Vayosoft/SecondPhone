using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata;
using Vayosoft.Identity;

namespace EmulatorHub.Infrastructure.Persistence.Filters
{
    public enum QueryFilterTypes { SoftDelete, ProviderId }
    public static class SoftDeleteQueryFilter
    {
        public static void AddSoftDeleteQueryFilter(this IMutableEntityType entityData,
            QueryFilterTypes queryFilterType, IUserContext? userContext = null)
        {
            var methodName = $"Get{queryFilterType}Filter";
            var methodToCall = typeof(SoftDeleteQueryFilter)
                .GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(entityData.ClrType);
            var filter = methodToCall.Invoke(null, new object[] {userContext});
            entityData.SetQueryFilter((LambdaExpression)filter);
        }
    }
}
