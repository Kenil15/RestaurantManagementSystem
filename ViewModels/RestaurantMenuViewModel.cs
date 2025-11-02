using ForUpworkRestaurentManagement.Models;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ForUpworkRestaurentManagement.ViewModels
{
    public class RestaurantMenuViewModel
    {
        [ValidateNever]
        public Restaurant Restaurant { get; set; } = null!;
        [ValidateNever]
        public List<MenuItem> MenuItems { get; set; } = new();

        // Booking form
        [DataType(DataType.Date)]
        public DateTime? BookingDate { get; set; }

        [DataType(DataType.Time)]
        public TimeSpan? BookingTime { get; set; }

        [Range(1, 20)]
        public int GuestsCount { get; set; } = 2;

        [ValidateNever]
        public List<TimeSpan> AvailableSlots { get; set; } = new();

        // Reviews
        [ValidateNever]
        public List<RestaurantReview> Reviews { get; set; } = new();
        [ValidateNever]
        public double AverageRating { get; set; }
    }
}
