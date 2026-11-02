using Xunit;
using NetProject.Services;

namespace BTL_CKy.UnitTests;

public sealed class SecurityHelperTests
{
    [Fact]
    public void HashPassword_ReturnsNonEmptyBcryptHash()
    {
        var hash = SecurityHelper.HashPassword("mySecretPassword");

        Assert.False(string.IsNullOrWhiteSpace(hash));
        Assert.StartsWith("$2", hash);
    }

    [Fact]
    public void HashPassword_SameInput_ReturnsDifferentHashEachTime()
    {
        var hash1 = SecurityHelper.HashPassword("password");
        var hash2 = SecurityHelper.HashPassword("password");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void VerifyPassword_CorrectPassword_ReturnsTrue()
    {
        string password = "TestP@ssw0rd!";
        string hash = SecurityHelper.HashPassword(password);

        Assert.True(SecurityHelper.VerifyPassword(password, hash));
    }

    [Fact]
    public void VerifyPassword_WrongPassword_ReturnsFalse()
    {
        string hash = SecurityHelper.HashPassword("correctPassword");

        Assert.False(SecurityHelper.VerifyPassword("wrongPassword", hash));
    }

    [Fact]
    public void VerifyPassword_EmptyInput_ReturnsFalse()
    {
        Assert.False(SecurityHelper.VerifyPassword("", "somehash"));
        Assert.False(SecurityHelper.VerifyPassword("  ", "somehash"));
        Assert.False(SecurityHelper.VerifyPassword("pass", ""));
        Assert.False(SecurityHelper.VerifyPassword("pass", "   "));
    }

    [Fact]
    public void VerifyPassword_NullInput_ReturnsFalse()
    {
        Assert.False(SecurityHelper.VerifyPassword(null!, "somehash"));
        Assert.False(SecurityHelper.VerifyPassword("pass", null!));
    }

    [Fact]
    public void VerifyPassword_BcryptHash_ValidatesCorrectly()
    {
        string password = "Kho@2026Secure";
        string hash = SecurityHelper.HashPassword(password);

        Assert.True(SecurityHelper.VerifyPassword(password, hash));
        Assert.False(SecurityHelper.VerifyPassword("Kho@2026Wrong", hash));
    }

    [Fact]
    public void VerifyPassword_SeedAccounts_WorkWithDefaultHash()
    {
        string seedHash = "$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi";

        Assert.True(SecurityHelper.VerifyPassword("password", seedHash));
        Assert.True(SecurityHelper.VerifyPassword("admin", seedHash));
    }

    [Fact]
    public void HashPassword_OutputCanBeVerified()
    {
        var passwords = new[] { "abc123", "P@ssw0rd!", "tiếng_việt_mật_khẩu", "a" };

        foreach (var pwd in passwords)
        {
            string hash = SecurityHelper.HashPassword(pwd);
            Assert.True(SecurityHelper.VerifyPassword(pwd, hash));
        }
    }

    [Fact]
    public void VerifyPassword_PlaintextFallback_MatchesExactString()
    {
        Assert.True(SecurityHelper.VerifyPassword("directmatch", "directmatch"));
        Assert.False(SecurityHelper.VerifyPassword("directmatch", "DIRECTMATCH"));
    }
}
