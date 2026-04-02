using Shared.Contracts.Returns;

namespace Ops.Bff.Presenters;

public static class ReturnWorkbenchPresenter
{
    public static DispositionSuggestionDto CoalesceSuggestion(
        DispositionSuggestionDto? runtimeSuggestion,
        Guid returnOrderId) =>
        runtimeSuggestion ?? CreateUnavailableSuggestion(returnOrderId);

    public static DispositionSuggestionDto CreateUnavailableSuggestion(Guid returnOrderId) =>
        new(
            returnOrderId,
            "Unavailable",
            "Unknown",
            [],
            "Unavailable");
}
