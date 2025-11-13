using FluentAssertions;
using InvoiceRobot.Core.Domain;

namespace InvoiceRobot.Core.Tests.Domain;

public class ApprovalRequestTests
{
    [Fact]
    public void ApprovalRequest_Should_Initialize_Pending()
    {
        var request = new ApprovalRequest();
        request.Status.Should().Be(ApprovalStatus.Pending);
    }

    [Fact]
    public void ApprovalRequest_Should_Generate_Unique_Token()
    {
        var request1 = new ApprovalRequest { Token = Guid.NewGuid().ToString() };
        var request2 = new ApprovalRequest { Token = Guid.NewGuid().ToString() };

        request1.Token.Should().NotBe(request2.Token);
    }
}
