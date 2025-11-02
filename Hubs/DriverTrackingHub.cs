using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ForUpworkRestaurentManagement.Hubs
{
    public class DriverTrackingHub : Hub
    {
        public async Task JoinOrderGroup(string orderId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GetGroup(orderId));
        }

        [Authorize(Roles = "Admin")]
        public async Task UpdateLocation(string orderId, double lat, double lng, string? status = null)
        {
            await Clients.Group(GetGroup(orderId)).SendAsync("DriverLocationUpdated", new { lat, lng });
            if (!string.IsNullOrWhiteSpace(status))
            {
                await Clients.Group(GetGroup(orderId)).SendAsync("OrderStatusUpdated", status);
            }
        }

        private static string GetGroup(string orderId) => $"order-{orderId}";
    }
}


