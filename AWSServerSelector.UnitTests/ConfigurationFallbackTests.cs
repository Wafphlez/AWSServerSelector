using System;
using System.IO;
using AWSServerSelector.Models;
using AWSServerSelector.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace AWSServerSelector.UnitTests;

public class ConfigurationFallbackTests
{
    [Fact]
    public void RegionCatalogService_UsesFallback_WhenOptionsAreEmpty()
    {
        var options = Options.Create(new RegionCatalogOptions());
        var service = new RegionCatalogService(options);

        Assert.True(service.Regions.ContainsKey("Europe (Frankfurt am Main)"));
    }

    [Fact]
    public void RegionCatalogService_UsesConfiguredRegions_WhenValidDataProvided()
    {
        var options = Options.Create(new RegionCatalogOptions
        {
            Regions =
            [
                new RegionDefinition(
                    "Custom Region",
                    "Custom Group",
                    "Custom Group Display",
                    ["example.org"],
                    true,
                    "Custom_Region")
            ]
        });

        var service = new RegionCatalogService(options);

        Assert.True(service.Regions.ContainsKey("Custom Region"));
        Assert.Equal(1, service.GetGroupOrder("Custom Group"));
    }

    [Fact]
    public void HostsService_UsesFallbackTemplate_WhenConfiguredPathMissing()
    {
        var options = Options.Create(new HostsOptions
        {
            DefaultHostsTemplatePath = "Config/does-not-exist.txt"
        });

        var service = new HostsService(options);
        var template = service.ReadDefaultTemplate();

        Assert.Contains("localhost name resolution is handled within DNS itself.", template);
    }

    [Fact]
    public void HostsService_ReadsTemplateFromConfiguredPath_WhenFileExists()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"hosts-template-{Guid.NewGuid():N}.txt");
        try
        {
            File.WriteAllText(tempFile, "# custom hosts template");

            var options = Options.Create(new HostsOptions
            {
                DefaultHostsTemplatePath = tempFile
            });

            var service = new HostsService(options);
            var template = service.ReadDefaultTemplate();

            Assert.Equal("# custom hosts template", template);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}
