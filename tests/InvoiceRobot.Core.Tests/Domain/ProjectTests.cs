using FluentAssertions;
using InvoiceRobot.Core.Domain;

namespace InvoiceRobot.Core.Tests.Domain;

public class ProjectTests
{
    [Fact]
    public void Project_Should_Initialize_Active()
    {
        var project = new Project();
        project.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Project_Should_Set_Properties()
    {
        var project = new Project
        {
            ProjectCode = "PRJ-001",
            Name = "Kerrostalo Mannerheimintie",
            Address = "Mannerheimintie 123, Helsinki",
            NetvisorProjectKey = 12345
        };

        project.ProjectCode.Should().Be("PRJ-001");
        project.Name.Should().Be("Kerrostalo Mannerheimintie");
        project.NetvisorProjectKey.Should().Be(12345);
    }
}
