using System.ComponentModel.DataAnnotations;

namespace ForUpworkRestaurentManagement.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public string Description { get; set; }= string.Empty;

        // Navigation property
        public virtual ICollection<MenuItem> MenuItems { get; set; }= new List<MenuItem>();
    }
}
