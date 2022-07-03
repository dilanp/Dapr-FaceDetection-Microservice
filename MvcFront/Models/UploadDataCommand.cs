using System;
using Microsoft.AspNetCore.Http;

namespace MvcFront.Models
{
    public class UploadDataCommand
    {
        public Guid OrderId { get; set; }
        public string UserEmail { get; set; }
        public string PhotoUrl { get; set; }
        public IFormFile File { get; set; }

    }
}
