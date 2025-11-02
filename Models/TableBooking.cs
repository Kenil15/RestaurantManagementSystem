using System.ComponentModel.DataAnnotations;

namespace ForUpworkRestaurentManagement.Models
{
    public class TableBooking
    {
        public int Id { get; set; }

        [Required]
        public int RestaurantId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        public DateTime BookingDate { get; set; }

        [Required]
        [DataType(DataType.Time)]
        public TimeSpan BookingTime { get; set; }

        [Range(1, 20)]
        public int GuestsCount { get; set; }

        [StringLength(30)]
        public string Status { get; set; } = "Pending"; // Pending, Confirmed, Rejected

        public virtual Restaurant Restaurant { get; set; } = null!;
    }
}
