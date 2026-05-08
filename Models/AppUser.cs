namespace TL.Models
{
    /// <summary>
    /// Represents the currently authenticated Windows user.
    /// Populated from HttpContext.User (Negotiate/NTLM/Kerberos).
    /// </summary>
    public class AppUser
    {
        /// <summary>SAM account name, e.g. "jsmith"</summary>
        public string Username { get; init; } = "";

        /// <summary>Display name from AD, e.g. "James Smith"</summary>
        public string DisplayName { get; init; } = "";

        /// <summary>True if the user is in the Managers or Directors AD group</summary>
        public bool IsManager { get; init; }
    }
}
