using System.Security.Principal;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Vayosoft.Identity;
using Vayosoft.Identity.Extensions;
using Vayosoft.Identity.Persistence;
using Vayosoft.Identity.Security;

namespace EmulatorHub.Application.Commons.Services
{
    public class UserContext : IUserContext
    {
        public const string SupervisorID = "f6694d71d26e40f5a2abb357177c9bdz";
        public const string AdministratorID = "f6694d71d26e40f5a2abb357177c9bdx";
        public const string SupportID = "f6694d71d26e40f5a2abb357177c9bdt";

        private readonly IHttpContextAccessor _httpContextAccessor;
        protected HttpContext HttpContext => _httpContextAccessor.HttpContext;

        public UserContext(IHttpContextAccessor httpAccessor)
        {
            _httpContextAccessor = httpAccessor;

            Session = new UserSession(httpAccessor);
        }

        public IPrincipal User => HttpContext?.User;
        public IUserSession Session { get; }

        public List<RoleDTO> Roles { get; private set; }

        public async ValueTask<bool> LoadContextAsync()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context?.User.Identity == null)
                return await ValueTask.FromResult(false);

            List<RoleDTO> userRoles;
            if ((userRoles = await context.Session.GetAsync<List<RoleDTO>>("_roles")) == null)
            {
                var userRepository = context.RequestServices.GetRequiredService<IUserRepository>();

                userRoles = await userRepository.
                    GetUserRolesAsync(context.User.Identity.GetUserId(), context.RequestAborted);
                await context.Session.SetAsync("_roles", userRoles);
            }
            Roles = userRoles;
            return userRoles != null;
        }

        public bool IsSupervisor =>
            User.Identity?.GetUserType() == UserType.Supervisor || User.IsInRole(SupervisorID);
        public bool IsAdministrator =>
            IsSupervisor || User.Identity?.GetUserType() == UserType.Administrator || User.IsInRole(AdministratorID);

        public bool HasRole(string role)
        {
            return Roles != null && Roles.Any(r => r.Name.Equals(role, StringComparison.InvariantCultureIgnoreCase));
        }

        public bool HasAnyRole(IEnumerable<string> roles)
        {
            if (IsSupervisor)
                return true;

            if (Roles == null || Roles.Count == 0)
                return false;

            foreach (var role in roles)
            {
                if (HasRole(role))
                    return true;
            }

            return false;
        }

        public bool HasPermission(string objName, SecurityPermissions requiredPermissions)
        {
            if (IsAdministrator)
                return true;

            foreach (var r in Roles)
            {
                if (r.Items == null || r.Items.Count == 0)
                    continue;

                if (r.Items.Any(item => item.ObjectName.Equals(objName, StringComparison.CurrentCultureIgnoreCase)
                                        && item.Permissions.HasFlag(requiredPermissions)))
                    return true;
            }

            return false;
        }
    }
}
