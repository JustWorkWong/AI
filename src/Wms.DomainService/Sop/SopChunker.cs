using System.Text.RegularExpressions;

namespace Wms.DomainService.Sop;

public static partial class SopChunker
{
    private const string StepPattern = @"(?<step>STEP-[^|]+)\|(?<content>.*?)(?=(STEP-[^|]+\|)|$)";

    public static IReadOnlyList<SopDraftChunk> Split(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return [];
        }

        var matches = StepRegex().Matches(source);
        if (matches.Count == 0)
        {
            return [];
        }

        var chunks = new List<SopDraftChunk>(matches.Count);
        for (var index = 0; index < matches.Count; index++)
        {
            var match = matches[index];
            var stepCode = match.Groups["step"].Value.Trim();
            var content = match.Groups["content"].Value.Trim();

            if (stepCode.Length == 0 || content.Length == 0)
            {
                continue;
            }

            chunks.Add(new SopDraftChunk(stepCode, index + 1, content));
        }

        return chunks;
    }

    [GeneratedRegex(StepPattern, RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex StepRegex();
}

public sealed record SopDraftChunk(string StepCode, int Sequence, string Content);
