using WrapPassword.Domain.Passwords;

namespace WrapPassword.Application.Services;

public sealed class PasswordDictionaryGenerator
{
    public IEnumerable<string> Generate()
    {
        var candidate = new char[PasswordRules.CharacterOptions.Count];
        return GenerateAtPosition(0, candidate);
    }

    private static IEnumerable<string> GenerateAtPosition(int position, char[] candidate)
    {
        if (position == PasswordRules.CharacterOptions.Count)
        {
            yield return new string(candidate);
            yield break;
        }

        foreach (var character in PasswordRules.CharacterOptions[position])
        {
            candidate[position] = character;

            foreach (var password in GenerateAtPosition(position + 1, candidate))
            {
                yield return password;
            }
        }
    }
}
