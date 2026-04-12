namespace LimitlessNonsense;

/// <summary>
/// Provides summarisation of a block of text
/// </summary>
public interface ISummarisationProvider
{
    /// <summary>
    /// Given a block of input text, summarise/compress it.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    Task<string> Summarise(string input);
}