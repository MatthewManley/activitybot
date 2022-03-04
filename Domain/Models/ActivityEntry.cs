using System;

namespace Domain.Models
{
    public class ActivityEntry
    {
        public ulong Server { get; set; }
        public ulong User { get; set; }
        public DateTime LastActivity { get; set; }
        public ActivityEntryStatus RemovalStatus { get; set; }
    }

    public enum ActivityEntryStatus
    {
        Assigned = 0,
        Removed = 1,
        //PendingRemoval = 2,
        RemovalError = 3,
        //PendingDeletion = 4,
        DeletionError = 5,
    }
}
