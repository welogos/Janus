using Janus.Domain.Models;

namespace Janus.Domain.Tests.Models;

public class EntityTests
{
    #region Fact Tests
    [Fact]
    public void WhenNewValueEqualsCurrentValueHasNotUpdatedEntity()
    {
        var entity = new Entity();
        var updateWasCalled = false;

        entity.Update("same", "same", () => updateWasCalled = true);

        Assert.False(updateWasCalled);
        Assert.Equal(default, entity.UpdatedAt);
    }

    [Fact]
    public void WhenNewValueDiffersFromCurrentValueHasUpdatedEntity()
    {
        var entity = new Entity();
        var updateWasCalled = false;
        var startedAt = DateTime.UtcNow;

        entity.Update("new", "current", () => updateWasCalled = true);

        var finishedAt = DateTime.UtcNow;

        Assert.True(updateWasCalled);
        Assert.InRange(entity.UpdatedAt, startedAt, finishedAt);
        Assert.Equal(DateTimeKind.Utc, entity.UpdatedAt.Kind);
    }
    #endregion
}
