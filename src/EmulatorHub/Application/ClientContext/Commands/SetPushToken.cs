using System.ComponentModel.DataAnnotations;

namespace EmulatorHub.Application.ClientContext.Commands
{
    public class SetPushToken
    {
        [Required]
        [MaxLength(500)]
        public string Token { get; set; }
    }
}
