namespace TL.Models
{
    public class AppUser
    {
        public string Username { get; init; } = "";
        public string DisplayName { get; init; } = "";
        public bool IsManager { get; init; }
    }
}
