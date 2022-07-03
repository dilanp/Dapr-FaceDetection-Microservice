using System;
using System.Collections.Generic;

namespace OrdersApi.Commands
{
    public class OrderStatusChangedToProcessedCommand
    {
        public Guid OrderId { get; set; }
        public List<byte[]> Faces { get; set; }
    }
}
