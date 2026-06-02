using FluentAssertions;
using Moq;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Services;
using TaskManagement.Domain.Interfaces;

namespace TaskManagement.Tests.Services;

public class DashboardServiceTests
{
    [Fact]
    public async Task GetSummaryAsync_ReturnsTenantCounts()
    {
        var tenantId = Guid.NewGuid();
        var taskRepository = new Mock<ITaskRepository>();
        var currentUser = new Mock<ICurrentUserService>();

        currentUser.Setup(c => c.TenantId).Returns(tenantId);
        taskRepository.Setup(r => r.GetTenantTaskSummaryAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TenantTaskSummary(10, 4, 6));

        var sut = new DashboardService(taskRepository.Object, currentUser.Object);
        var result = await sut.GetSummaryAsync();

        result.TotalTasks.Should().Be(10);
        result.CompletedTasks.Should().Be(4);
        result.PendingTasks.Should().Be(6);
    }
}
