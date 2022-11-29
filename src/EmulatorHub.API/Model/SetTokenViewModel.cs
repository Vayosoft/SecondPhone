using System.ComponentModel.DataAnnotations;

namespace EmulatorHub.API.Model
{
    public class SetTokenViewModel
    {
        [Required]
        [MaxLength(100)]
        public string Token { get; set; }
    }
}
