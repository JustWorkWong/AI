using Ops.Bff.Presenters;
using Shared.Contracts.Returns;

namespace Ops.Bff.Tests;

public sealed class ReturnWorkbenchPresenterTests
{
    [Fact]
    public void Create_unavailable_suggestion_should_use_stable_placeholder_shape()
    {
        var returnOrderId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var suggestion = ReturnWorkbenchPresenter.CreateUnavailableSuggestion(returnOrderId);

        Assert.Equal(returnOrderId, suggestion.ReturnOrderId);
        Assert.Equal("Unavailable", suggestion.SuggestedOutcome);
        Assert.Equal("Unknown", suggestion.RiskLevel);
        Assert.Equal("Unavailable", suggestion.ApprovalStatus);
        Assert.Empty(suggestion.Citations);
    }

    [Fact]
    public void Coalesce_suggestion_should_return_runtime_payload_when_available()
    {
        var runtimeSuggestion = new DispositionSuggestionDto(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "Scrap",
            "High",
            [],
            "Pending");

        var suggestion = ReturnWorkbenchPresenter.CoalesceSuggestion(runtimeSuggestion, runtimeSuggestion.ReturnOrderId);

        Assert.Same(runtimeSuggestion, suggestion);
    }
}
