using FolderLocker.Models;

namespace FolderLocker.Tests;

public class PasswordStrengthTests
{
    [Theory]
    [InlineData("", StrengthLevel.Empty)]
    [InlineData("abc", StrengthLevel.TooShort)]
    [InlineData("1234567", StrengthLevel.TooShort)]
    [InlineData("password", StrengthLevel.Weak)]
    [InlineData("Matkhau123", StrengthLevel.Weak)]       // danh sách phổ biến, không phân biệt hoa thường
    [InlineData("aaaaaaaaaaaa", StrengthLevel.Weak)]     // lặp
    [InlineData("abcdefghij", StrengthLevel.Weak)]       // dãy liên tiếp
    [InlineData("hoahongdo", StrengthLevel.Weak)]        // 1 nhóm ký tự, ngắn
    [InlineData("Hoahongdo7", StrengthLevel.Fair)]
    [InlineData("Hoa-Hong-Do-7", StrengthLevel.Strong)]
    [InlineData("Tr4ng-Ch0-Bi3t!", StrengthLevel.Strong)]
    [InlineData("Tr4ng-Ch0-Bi3t-Nhe-2024!", StrengthLevel.VeryStrong)]
    [InlineData("cay xanh bên hồ lúc bình minh", StrengthLevel.Strong)]
    public void Evaluates_expected_level(string password, StrengthLevel expected)
    {
        Assert.Equal(expected, PasswordStrength.Evaluate(password));
    }

    [Fact]
    public void Every_level_has_text_and_increasing_percent()
    {
        var levels = new[] { StrengthLevel.TooShort, StrengthLevel.Weak, StrengthLevel.Fair, StrengthLevel.Strong, StrengthLevel.VeryStrong };
        Assert.All(levels, l => Assert.False(string.IsNullOrEmpty(PasswordStrength.Describe(l))));
        Assert.Equal(levels.Select(PasswordStrength.Percent).Order(), levels.Select(PasswordStrength.Percent));
    }
}
