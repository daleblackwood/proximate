using Xunit;

namespace Proximate.Tests;

public class CompressionTest
{
    [Fact]
    public void CompressionGoesBothWays()
    {
        var str = "178a56e7-349c-4066-9d62-58841c44fdfa\\nf22a0fec-4271-4e77-8329-fd83a8e2c797\\n69bd99c7-37f0-4274-9d47-a2d0428635fa\\n1e695633-08cf-4060-bd1c-a1d5a0466f67\\n2e7b2561-26d4-40eb-a1a7-bc3d266db7d0\\n1d0c0fb6-a46f-47de-a587-c3c713de5b47\\n2d67a662-82fb-45a7-8b31-cf25e75288b6\\nae012e3b-f281-467b-8d5d-12236e181c9d\\n863bf040-beae-4650-8e8e-515a5576bb36\\neb1f1dc7-3098-4b01-a6b6-dc2ae05449b0";
        Assert.False(Compression.IsCompressed(str), "raw string should not be compressed.");
        
        var compressed = Compression.Compress(str);
        Assert.True(Compression.IsCompressed(compressed), "compressed string should be compressed.");

        var decompressed = Compression.Decompress(compressed);
        Assert.False(Compression.IsCompressed(decompressed), "decompressed string should not be compressed.");
        Assert.Equal(decompressed, str);       
    }

}