using System.Text.Json.Serialization;

namespace AppliedSoftware.Models.Response;

/// <summary>
/// A container to store results with a counter of the number of items.
/// </summary>
/// <typeparam name="T"></typeparam>
public sealed class CounterResult<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CounterResult{T}"/> class.
    /// </summary>
    public CounterResult()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CounterResult{T}"/> class.
    /// </summary>
    /// <param name="results">The results.</param>
    public CounterResult
        (
            IEnumerable<T> results
        )
    {
        Count = results.Count();
        Results = results;
    }

    /// <summary>
    /// The count of results.
    /// </summary>
    [JsonPropertyName("count")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int? Count { get; set; } = 0;

    /// <summary>
    /// The Results.
    /// </summary>
    [JsonPropertyName("results")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public IEnumerable<T> Results { get; set; } = default!;
}