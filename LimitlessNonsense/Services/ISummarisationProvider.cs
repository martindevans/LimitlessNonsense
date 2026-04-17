namespace LimitlessNonsense.Services;

/// <summary>
/// Provides summarisation of a block of text
/// </summary>
public interface ISummarisationProvider
{
    /// <summary>
    /// Given a block of input text, summarise/compress it.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="cancellation"></param>
    /// <returns></returns>
    Task<string> Summarise(string input, CancellationToken cancellation = default);
}