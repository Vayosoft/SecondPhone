using EmulatorHub.Application.Administration.Models;
using EmulatorHub.Application.Administration.Specifications;
using EmulatorHub.Domain.Commons.Entities;
using Microsoft.Extensions.DependencyInjection;
using Vayosoft.Commons.Models.Pagination;
using Vayosoft.Persistence.Queries;
using Vayosoft.Queries;

namespace EmulatorHub.Application.Administration
{
    public static class Configurations
    {
        public static IServiceCollection AddAdministrationService(this IServiceCollection services) =>
            services
                .AddQueryHandlers()
                .AddCommandHandlers();

        private static IServiceCollection AddQueryHandlers(this IServiceCollection services) =>
            services
                .AddQueryHandler<SpecificationQuery<EmulatorSpec, IPagedEnumerable<EmulatorDto>>, IPagedEnumerable<EmulatorDto>,
                    PagingQueryHandler<string, EmulatorSpec, Emulator, EmulatorDto>>();

        private static IServiceCollection AddCommandHandlers(this IServiceCollection services) =>
            services
        ;
    }
}
