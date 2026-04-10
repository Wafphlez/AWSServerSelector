using AWSServerSelector.Services;
using Xunit;

namespace AWSServerSelector.UnitTests;

public class VersionComparerTests
{
    [Fact]
    public void Compare_ReturnsPositive_WhenLeftIsGreater()
    {
        Assert.True(VersionComparer.Compare("1.2.0", "1.1.9") > 0);
    }

    [Fact]
    public void Compare_ReturnsNegative_WhenLeftIsLower()
    {
        Assert.True(VersionComparer.Compare("1.0.0", "1.0.1") < 0);
    }

    [Fact]
    public void Compare_ReturnsZero_WhenVersionsEqual()
    {
        Assert.Equal(0, VersionComparer.Compare("1.2.3", "1.2.3"));
    }

    [Fact]
    public void Compare_HandlesSuffixes()
    {
        Assert.Equal(0, VersionComparer.Compare("v1.2.3-beta", "1.2.3"));
    }
}
