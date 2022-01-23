namespace Infrastructure
{
    public class DbOptions
    {
        public string Server { get; set; }
        public string UserId { get; set; }
        public string Password { get; set; }
        public string Database { get; set; }
        public string ConnectionString => $"server={Server};uid={UserId};pwd={Password};database={Database};";
    }
}
