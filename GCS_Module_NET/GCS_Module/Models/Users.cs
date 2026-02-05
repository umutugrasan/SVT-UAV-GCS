using System;
using System.ComponentModel.DataAnnotations;

namespace GCS_Module.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Role { get; set; } = "Operator";

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? LastLogin { get; set; }
    }

    public class LoginViewModel
    {
        [Required(ErrorMessage = "Kullanıcı adı gerekli")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre gerekli")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }
}