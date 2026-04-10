using AWSServerSelector.ViewModels;
using Xunit;

namespace AWSServerSelector.UnitTests;

public class SettingsDialogViewModelTests
{
    [Fact]
    public void ResetDefaults_RestoresExpectedValues()
    {
        var vm = new SettingsDialogViewModel
        {
            SelectedLanguage = "ru",
            SelectedMode = "service",
            IsBlockBoth = false,
            IsBlockPing = true,
            IsBlockService = false,
            IsMergeUnstable = true
        };

        vm.ResetDefaults();

        Assert.Equal("en", vm.SelectedLanguage);
        Assert.Equal("hosts", vm.SelectedMode);
        Assert.True(vm.IsBlockBoth);
        Assert.False(vm.IsBlockPing);
        Assert.False(vm.IsBlockService);
        Assert.False(vm.IsMergeUnstable);
    }
}
