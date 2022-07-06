using System;

namespace NotificationApi.Events
{
    public class OrderDispatchedEvent
    {
        public Guid OrderId { get; set; }
        public DateTime DispatchDateTime { get; set; }
    }
}
