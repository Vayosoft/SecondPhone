﻿using Vayosoft.Commons.Entities;
using Vayosoft.Identity;

namespace EmulatorHub.Domain.Entities
{
    public class DeviceEntity : EntityBase<string>, IProviderId<long>
    {
        public string? Name { get; set; }
        public UserEntity User { get; set; } = null!;

        public long ProviderId { get; set; }
        object IProviderId.ProviderId => ProviderId;
    }
}
