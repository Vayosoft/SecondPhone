using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata;
using Vayosoft.Commons.Entities;
using Vayosoft.Identity;
using Vayosoft.Identity.Extensions;

namespace EmulatorHub.Infrastructure.Persistence.Filters
{
    public enum QueryFilterTypes { SoftDelete, ProviderId }
    public static class QueryFilters
    {
        public static void AddSoftDeleteQueryFilter(this IMutableEntityType entityData)
        {
            var methodToCall = GetMethodToCall(entityData, QueryFilterTypes.SoftDelete);
            var filter = methodToCall.Invoke(null, new object[] { })!;
            entityData.SetQueryFilter((LambdaExpression)filter);
            entityData.AddIndex(entityData.FindProperty(nameof(ISoftDelete.SoftDeleted))!);
        }

        public static void AddProviderIdQueryFilter(this IMutableEntityType entityData, IUserContext userContext)
        {
            var methodToCall = GetMethodToCall(entityData, QueryFilterTypes.ProviderId);

            var filter = methodToCall.Invoke(null, new object[] { userContext })!;
            entityData.SetQueryFilter((LambdaExpression)filter);
            entityData.AddIndex(entityData.FindProperty((nameof(IProviderId.ProviderId)))!);
        }

        private static MethodInfo GetMethodToCall(IReadOnlyTypeBase entityData, QueryFilterTypes queryFilterType)
        {
            var methodName = $"Get{queryFilterType}Filter";

            return typeof(QueryFilters)
                .GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(entityData.ClrType);
        }

        private static LambdaExpression GetProviderIdFilter<TEntity>(IUserContext userContext)
            where TEntity : class, IProviderId
        {
            Expression<Func<TEntity, bool>> filter = x => (long)x.ProviderId == userContext.User.Identity.GetProviderId();
            return filter;
        }

        private static LambdaExpression GetSoftDeleteFilter<TEntity>(IUserContext userContext)
            where TEntity : class, ISoftDelete
        {
            Expression<Func<TEntity, bool>> filter = x => !x.SoftDeleted;
            return filter;
        }

    }
}
