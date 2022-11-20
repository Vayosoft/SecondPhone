using System.Security.Principal;
using Microsoft.AspNetCore.Http;
using Vayosoft.Identity;
using Vayosoft.Identity.Security;

namespace EmulatorHub.Application.Services
{
    public class UserContext : IUserContext
    {
        private readonly IHttpContextAccessor _httpAccessor;

        public UserContext(IHttpContextAccessor httpAccessor)
        {
            _httpAccessor = httpAccessor;
        }

        public IPrincipal User { get; }
        public Task<bool> LoadSessionAsync()
        {
            throw new NotImplementedException();
        }

        public bool HasRole(string role)
        {
            throw new NotImplementedException();
        }

        public bool HasAnyRole(IEnumerable<string> roles)
        {
            throw new NotImplementedException();
        }

        public bool HasPermission(string objName, SecurityPermissions requiredPermissions)
        {
            throw new NotImplementedException();
        }

        public bool IsSupervisor { get; }
        public bool IsAdministrator { get; }
        public T Get<T>(string key) where T : class
        {
            throw new NotImplementedException();
        }

        public void Set<T>(string key, T value) where T : class
        {
            throw new NotImplementedException();
        }

        public Task<T> GetAsync<T>(string key) where T : class
        {
            throw new NotImplementedException();
        }

        public Task SetAsync<T>(string key, T value) where T : class
        {
            throw new NotImplementedException();
        }

        public void SetBoolean(string key, bool value)
        {
            throw new NotImplementedException();
        }

        public bool? GetBoolean(string key)
        {
            throw new NotImplementedException();
        }

        public void SetDouble(string key, double value)
        {
            throw new NotImplementedException();
        }

        public double? GetDouble(string key)
        {
            throw new NotImplementedException();
        }

        public void SetInt64(string key, long value)
        {
            throw new NotImplementedException();
        }

        public long? GetInt64(string key)
        {
            throw new NotImplementedException();
        }

        public byte[] this[string index]
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }
    }
}
