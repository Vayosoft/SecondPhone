using EmulatorHub.Application.Administration.Models;
using EmulatorHub.Application.Administration.Specifications;
using EmulatorHub.Domain.Commons.Entities;
using Microsoft.Extensions.DependencyInjection;
using Vayosoft.Commands;
using Vayosoft.Commons.Models.Pagination;
using Vayosoft.Identity;
using Vayosoft.Identity.Providers;
using Vayosoft.Identity.Security.Commands;
using Vayosoft.Identity.Security.Models;
using Vayosoft.Identity.Security.Queries;
using Vayosoft.Persistence.Commands;
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
                .AddQueryHandler<SpecificationQuery<UserSpec, IPagedEnumerable<UserEntityDto>>, IPagedEnumerable<UserEntityDto>,
                    PagingQueryHandler<string, UserSpec, UserEntity, UserEntityDto>>()
                .AddQueryHandler<SingleQuery<UserEntityDto>, UserEntityDto, SingleQueryHandler<long, UserEntity, UserEntityDto>>()
                .AddQueryHandler<GetPermissions, RolePermissions, HandleGetPermissions>()

                .AddQueryHandler<SpecificationQuery<EmulatorSpec, IPagedEnumerable<EmulatorDto>>, IPagedEnumerable<EmulatorDto>,
                    PagingQueryHandler<string, EmulatorSpec, Emulator, EmulatorDto>>();

        private static IServiceCollection AddCommandHandlers(this IServiceCollection services) =>
            services
                .AddCommandHandler<SaveUser, HandleSaveUser>()
                .AddCommandHandler<DeleteCommand<UserEntity>, DeleteCommandHandler<long, UserEntity>>()

                .AddCommandHandler<SavePermissions, HandleSavePermissions>()
                .AddCommandHandler<SaveRole, HandleSaveRole>()

                .AddCommandHandler<DeleteCommand<ProviderEntity>, DeleteCommandHandler<long, ProviderEntity>>()
                .AddCommandHandler<CreateOrUpdateCommand<ProviderEntity>, CreateOrUpdateHandler<long, ProviderEntity, ProviderEntity>>()
        ;
    }
}
