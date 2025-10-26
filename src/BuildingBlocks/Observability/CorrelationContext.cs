using System.Threading;

namespace BuildingBlocks.Observability;

public static class CorrelationContext
{
    private static readonly AsyncLocal<string?> _id = new();

    public static string Id
    {
        get
        {
            if (string.IsNullOrEmpty(_id.Value))
                _id.Value = Guid.NewGuid().ToString();
            return _id.Value!;
        }
    }

    public static void Set(string id) => _id.Value = id;
}
