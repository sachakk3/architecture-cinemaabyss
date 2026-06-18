namespace CinemaAbyss.Proxy.Domain;

/// <summary>
/// Named identifiers of upstream backends.
/// Used as HttpClient names and log labels.
/// </summary>
public static class Backends
{
    public const string Monolith = "monolith";
    public const string Movies   = "movies";
}
