namespace WrapPassword.Domain.Passwords;

public static class PasswordRules
{
    private static readonly IReadOnlyList<string> Options = Array.AsReadOnly(
    [
        "pP",
        "aA@",
        "sS5",
        "sS5",
        "wW",
        "oO0",
        "rR",
        "dD",
    ]);

    public const int ExpectedCandidateCount = 1_296;

    public static IReadOnlyList<string> CharacterOptions => Options;
}
