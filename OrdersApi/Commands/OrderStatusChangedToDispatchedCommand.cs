using System;

namespace OrdersApi.Commands
{
    public class OrderStatusChangedToDispatchedCommand
    {
        public Guid OrderId { get; set; }
        public DateTime DispatchDateTime { get; set; }
    }
}
