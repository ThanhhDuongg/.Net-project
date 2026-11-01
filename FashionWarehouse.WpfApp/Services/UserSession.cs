namespace FashionWarehouse.WpfApp.Services;

public static class UserSession
{
    public static int UserId { get; private set; }
    public static string Username { get; private set; } = string.Empty;
    public static string FullName { get; private set; } = string.Empty;
    public static string RoleName { get; private set; } = string.Empty;
    public static bool IsLoggedIn { get; private set; }

    public static void Login(int userId, string username, string fullName, string roleName)
    {
        UserId = userId;
        Username = username;
        FullName = fullName;
        RoleName = roleName;
        IsLoggedIn = true;
    }

    public static void Logout()
    {
        UserId = 0;
        Username = string.Empty;
        FullName = string.Empty;
        RoleName = string.Empty;
        IsLoggedIn = false;
    }
}

