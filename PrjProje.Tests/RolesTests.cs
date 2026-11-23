using PrjProje.Application.Common;

namespace PrjProje.Tests;

public class RolesTests
{
    [Theory]
    [InlineData(new[] { Roles.Employee }, Roles.Employee)]
    [InlineData(new[] { Roles.Marketing }, Roles.Marketing)]
    [InlineData(new[] { Roles.Employee, Roles.Admin }, Roles.Admin)]
    [InlineData(new[] { Roles.HumanResources, Roles.RnD }, Roles.RnD)]
    [InlineData(new string[0], Roles.Editor)]
    public void Primary_picks_the_widest_role(string[] held, string expected) =>
        Assert.Equal(expected, Roles.Primary(held.Contains));

    [Fact]
    public void Every_role_has_its_own_name() =>
        Assert.Equal(Roles.All.Count, Roles.All.Select(Roles.Name).Distinct().Count());
}
