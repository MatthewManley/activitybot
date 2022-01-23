using System;

namespace Domain.Models
{
    public class ActivityEntry
    {
        public ulong Server { get; set; }
        public ulong User { get; set; }
        public DateTime LastActivity { get; set; }
    }
}
