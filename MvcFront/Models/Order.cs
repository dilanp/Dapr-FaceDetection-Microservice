using System;

namespace MvcFront.Models
{
    public class Order
    {
        public Guid OrderId { get; set; }
        public string PhotoUrl { get; set; }
        public string UserEmail { get; set; }
        public Status Status { get; set; }
    }
}
