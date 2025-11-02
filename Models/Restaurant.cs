using System.ComponentModel.DataAnnotations;

namespace ForUpworkRestaurentManagement.Models
{
    public class Restaurant
    {
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string Location { get; set; } = string.Empty;

        [StringLength(300)]
        public string Address { get; set; } = string.Empty;

        [StringLength(50)]
        public string Contact { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Url]
        [StringLength(500)]
        public string? ImageUrl { get; set; }

        [DataType(DataType.Time)]
        public TimeSpan OpeningTime { get; set; } = new TimeSpan(10, 0, 0);

        [DataType(DataType.Time)]
        public TimeSpan ClosingTime { get; set; } = new TimeSpan(22, 0, 0);

        [Range(1, 100)]
        public int SlotCapacity { get; set; } = 10;

        public bool IsActive { get; set; } = true;

        public virtual ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
    }
}


