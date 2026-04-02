namespace Wms.DomainService.Returns;

public static class DispositionPolicy
{
    public static string Resolve(string condition) =>
        condition switch
        {
            "Broken" => "Scrap",
            "Unopened" => "Resell",
            _ => "ManualReview"
        };
}
