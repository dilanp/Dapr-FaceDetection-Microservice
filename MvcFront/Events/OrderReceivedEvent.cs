using System;

namespace MvcFront.Events
{
    public class OrderReceivedEvent
    {
        public OrderReceivedEvent(Guid orderId, string userEmail, string photoUrl, byte[] imageData)
        {
            OrderId = orderId;
            UserEmail = userEmail;
            PhotoUrl = photoUrl;
            ImageData = imageData;
        }

        public Guid OrderId { get; set; }
        public string UserEmail { get; set; }
        public string PhotoUrl { get; set; }
        public byte[] ImageData { get; set; }

    }
}
