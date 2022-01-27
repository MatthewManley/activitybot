namespace Domain.Models
{
    public class ServerConfig
    {
        public ulong ServerId { get; set; }
        public long Duration { get; set; }
        public ulong? Role { get; set; }
    }
}
