using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AWSServerSelector.Models;
using AWSServerSelector.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace AWSServerSelector.UnitTests;

public class ConfigurationFallbackTests
{
    [Fact]
    public void RegionCatalogService_Throws_WhenOptionsAreEmpty()
    {
        var options = Options.Create(new RegionCatalogOptions());

        Assert.Throws<InvalidOperationException>(() => new RegionCatalogService(options));
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
    public void RegionCatalogService_Throws_WhenDuplicateKeysFound()
    {
        var options = Options.Create(new RegionCatalogOptions
        {
            Regions =
            [
                new RegionDefinition("Dup", "G1", "Group 1", ["a.example"], true, "Dup_1"),
                new RegionDefinition("Dup", "G2", "Group 2", ["b.example"], true, "Dup_2")
            ]
        });

        Assert.Throws<InvalidOperationException>(() => new RegionCatalogService(options));
    }

    [Fact]
    public void HostsService_Throws_WhenConfiguredPathMissing()
    {
        var options = Options.Create(new HostsOptions
        {
            DefaultHostsTemplatePath = "Config/does-not-exist.txt"
        });

        var service = new HostsService(options);
        Assert.Throws<FileNotFoundException>(() => service.ReadDefaultTemplate());
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

    [Fact]
    public void HostsService_Throws_WhenTemplateFileIsEmpty()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"hosts-empty-{Guid.NewGuid():N}.txt");
        try
        {
            File.WriteAllText(tempFile, string.Empty);
            var options = Options.Create(new HostsOptions
            {
                DefaultHostsTemplatePath = tempFile
            });

            var service = new HostsService(options);
            Assert.Throws<InvalidOperationException>(() => service.ReadDefaultTemplate());
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void OptionsValidation_Throws_ForInvalidMonitoringAndLinks()
    {
        var configurationData = new Dictionary<string, string?>
        {
            ["AppLinks:DiscordUrl"] = "",
            ["Update:LatestReleaseApiUrl"] = "bad-url",
            ["Update:UserAgent"] = "",
            ["Update:TimeoutSeconds"] = "1",
            ["Monitoring:MainPingIntervalSeconds"] = "0",
            ["Monitoring:MainPingTimeoutMs"] = "50",
            ["Monitoring:ConnectionPollIntervalSeconds"] = "0",
            ["Monitoring:ConnectionPingTimeoutMs"] = "10",
            ["Monitoring:ConnectionGamePingIntervalSeconds"] = "0",
            ["Monitoring:IpApiTimeoutSeconds"] = "1",
            ["Hosts:DefaultHostsTemplatePath"] = "",
            ["RegionCatalog:Regions:0:Key"] = "R1",
            ["RegionCatalog:Regions:0:GroupKey"] = "G1",
            ["RegionCatalog:Regions:0:GroupDisplayName"] = "Group 1",
            ["RegionCatalog:Regions:0:DisplayNameKey"] = "R1_Display",
            ["RegionCatalog:Regions:0:Hosts:0"] = "r1.example"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData)
            .Build();

        var services = new ServiceCollection();
        services.AddOptions<AppLinksOptions>()
            .Bind(configuration.GetSection(AppLinksOptions.SectionName))
            .Validate(options =>
                !string.IsNullOrWhiteSpace(options.DiscordUrl) &&
                Uri.TryCreate(options.DiscordUrl, UriKind.Absolute, out _))
            .ValidateOnStart();

        services.AddOptions<UpdateOptions>()
            .Bind(configuration.GetSection(UpdateOptions.SectionName))
            .Validate(options =>
                !string.IsNullOrWhiteSpace(options.LatestReleaseApiUrl) &&
                Uri.TryCreate(options.LatestReleaseApiUrl, UriKind.Absolute, out _))
            .Validate(options => !string.IsNullOrWhiteSpace(options.UserAgent))
            .Validate(options => options.TimeoutSeconds >= 3 && options.TimeoutSeconds <= 120)
            .ValidateOnStart();

        services.AddOptions<MonitoringOptions>()
            .Bind(configuration.GetSection(MonitoringOptions.SectionName))
            .Validate(options => options.MainPingIntervalSeconds >= 1 && options.MainPingIntervalSeconds <= 120)
            .Validate(options => options.MainPingTimeoutMs >= 250 && options.MainPingTimeoutMs <= 10000)
            .Validate(options => options.ConnectionPollIntervalSeconds >= 1 && options.ConnectionPollIntervalSeconds <= 120)
            .Validate(options => options.ConnectionPingTimeoutMs >= 250 && options.ConnectionPingTimeoutMs <= 10000)
            .Validate(options => options.ConnectionGamePingIntervalSeconds >= 1 && options.ConnectionGamePingIntervalSeconds <= 10)
            .Validate(options => options.IpApiTimeoutSeconds >= 2 && options.IpApiTimeoutSeconds <= 30)
            .ValidateOnStart();

        services.AddOptions<HostsOptions>()
            .Bind(configuration.GetSection(HostsOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.DefaultHostsTemplatePath))
            .ValidateOnStart();

        services.AddOptions<RegionCatalogOptions>()
            .Bind(configuration.GetSection(RegionCatalogOptions.SectionName))
            .Validate(options => options.Regions.Count > 0)
            .Validate(options => options.Regions.All(region =>
                !string.IsNullOrWhiteSpace(region.Key) &&
                !string.IsNullOrWhiteSpace(region.GroupKey) &&
                !string.IsNullOrWhiteSpace(region.GroupDisplayName) &&
                !string.IsNullOrWhiteSpace(region.DisplayNameKey) &&
                region.Hosts.Length > 0 &&
                region.Hosts.All(host => !string.IsNullOrWhiteSpace(host))))
            .ValidateOnStart();

        var provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(() => _ = provider.GetRequiredService<IOptions<AppLinksOptions>>().Value);
    }
}
