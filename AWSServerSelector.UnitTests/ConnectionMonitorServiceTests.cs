using AWSServerSelector.Services;
using Xunit;

namespace AWSServerSelector.UnitTests;

public class ConnectionMonitorServiceTests
{
    [Fact]
    public async Task GetCurrentSnapshotAsync_ReturnsSnapshotWithLobbyAndGame()
    {
        var sut = new ConnectionMonitorService();

        var snapshot = await sut.GetCurrentSnapshotAsync();

        Assert.NotNull(snapshot);
        Assert.NotNull(snapshot.Lobby);
        Assert.NotNull(snapshot.Game);
        Assert.False(string.IsNullOrWhiteSpace(snapshot.LastUpdateText));
    }
}
