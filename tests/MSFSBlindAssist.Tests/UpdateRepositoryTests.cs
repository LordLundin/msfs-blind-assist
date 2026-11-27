// The update check reads the releases of the repository the build was published from: release.yml and
// preview.yml pass -p:UpdateRepository=${{ github.repository }}, the csproj writes it into assembly metadata,
// and UpdateService.Repository reads it back. A build with none (this test build) checks the default.

using MSFSBlindAssist.Services;

namespace MSFSBlindAssist.Tests;

public class UpdateRepositoryTests
{
    [Fact]
    public void The_releases_url_is_the_list_endpoint_of_the_given_repository()
        => Assert.Equal("https://api.github.com/repos/someone/msfs-blind-assist/releases?per_page=30",
            UpdateService.ReleasesApiUrl("someone/msfs-blind-assist"));

    [Fact]
    public void A_build_with_no_UpdateRepository_checks_the_default_repository()
        => Assert.Equal(UpdateService.DefaultRepository, UpdateService.Repository);

    [Theory]
    [InlineData("release.yml")]
    [InlineData("preview.yml")]
    public void The_workflow_build_passes_the_publishing_repository(string workflow)
    {
        string[] lines = File.ReadAllLines(Path.Combine(ClaudeContextBudgetTests.RepoRoot(), ".github", "workflows", workflow));
        Assert.Contains(lines, l => l.Contains("dotnet build MSFSBlindAssist.sln -c Release")
                                    && l.Contains("-p:UpdateRepository=${{ github.repository }}"));
    }
}
