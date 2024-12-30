using Microsoft.AspNetCore.Identity;

namespace Scanvirus.Models
{
    public class Users : IdentityUser
    {
        public string FullName { get; set; }
    }
}