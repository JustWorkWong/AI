using Wms.DomainService.Sop;

namespace Wms.DomainService.UnitTests;

public sealed class SopChunkerTests
{
    [Fact]
    public void Chunker_should_keep_step_code_in_each_chunk()
    {
        var chunks = SopChunker.Split("STEP-01|检查包装完整性。STEP-02|确认序列号。");

        Assert.Equal(2, chunks.Count);
        Assert.All(chunks, chunk => Assert.StartsWith("STEP-", chunk.StepCode));
        Assert.Equal(1, chunks[0].Sequence);
        Assert.Equal("检查包装完整性。", chunks[0].Content);
        Assert.Equal("STEP-02", chunks[1].StepCode);
    }
}
