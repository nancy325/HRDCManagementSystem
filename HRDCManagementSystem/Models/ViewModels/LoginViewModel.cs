using System.ComponentModel.DataAnnotations;

namespace HRDCManagementSystem.Models
{
    public class LoginViewModel
    {
        [Required]
        [Display(Name = "Username or Email")]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Recover Password")]
        public bool RecoverPassword { get; set; }
    }

}
