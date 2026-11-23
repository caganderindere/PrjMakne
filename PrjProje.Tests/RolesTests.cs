using PrjProje.Application.Common;

namespace PrjProje.Tests;

public class RolesTests
{
    [Fact]
    public void Only_admin_and_editor_exist() => Assert.Equal(["Admin", "Editor"], Roles.All);

    [Theory]
    [InlineData(new[] { Roles.Editor }, Roles.Editor)]
    [InlineData(new[] { Roles.Editor, Roles.Admin }, Roles.Admin)]
    [InlineData(new string[0], Roles.Editor)]
    public void Primary_picks_the_widest_role(string[] held, string expected) =>
        Assert.Equal(expected, Roles.Primary(held.Contains));

    [Fact]
    public void Every_role_has_its_own_name() =>
        Assert.Equal(Roles.All.Count, Roles.All.Select(Roles.Name).Distinct().Count());
}
