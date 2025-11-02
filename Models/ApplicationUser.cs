using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace ForUpworkRestaurentManagement.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(100)]
        public string LastName { get; set; }= string.Empty;

        public string Address { get; set; } = string.Empty;

        // Navigation properties
        public virtual ICollection<Order> Orders { get; set; }= new List<Order>();
    }
}
