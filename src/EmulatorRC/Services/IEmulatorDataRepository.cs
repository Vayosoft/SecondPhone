using EmulatorRC.Entities;

namespace EmulatorRC.Services
{
    public interface IEmulatorDataRepository
    {
        Task<Emulator> GetByClientIdAsync(string clientId);
    }
}

