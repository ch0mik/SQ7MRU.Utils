using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SQ7MRU.Utils.Infrastructure;

namespace SQ7MRU.Utils.Tests;

public sealed class ArtifactStorageOptionsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData("../outside")]
    [InlineData("nested/directory")]
    [InlineData("nested\\directory")]
    [InlineData("C:\\outside")]
    [InlineData("CON")]
    [InlineData("COM1.data")]
    [InlineData("name.")]
    [InlineData(" name")]
    public void RejectsUnsafeNamesBeforeResolvingStorage(string? name)
    {
        using var provider = new ServiceCollection()
            .Configure<ArtifactStorageOptions>(options => options.TechnicalDirectoryName = name!)
            .AddSq7MruUtils().BuildServiceProvider();
        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IArtifactStore>());
    }
}
