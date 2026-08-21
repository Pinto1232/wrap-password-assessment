using WrapPassword.Domain.Passwords;

namespace WrapPassword.Application.Services;

public static class PasswordDictionaryValidator
{
    public static IReadOnlyList<string> Validate(IEnumerable<string> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        var candidateList = candidates.ToArray();

        EnsureExpectedCount(candidateList);
        EnsureCandidatesAreUniqueAndValid(candidateList);

        return candidateList;
    }

    private static void EnsureExpectedCount(string[] candidates)
    {
        if (candidates.Length != PasswordRules.ExpectedCandidateCount)
        {
            throw new InvalidOperationException(
                $"The dictionary must contain exactly {PasswordRules.ExpectedCandidateCount:N0} candidates.");
        }
    }

    private static void EnsureCandidatesAreUniqueAndValid(IEnumerable<string> candidates)
    {
        var uniqueCandidates = new HashSet<string>(StringComparer.Ordinal);

        foreach (var candidate in candidates)
        {
            if (!IsValidCandidate(candidate))
            {
                throw new InvalidOperationException(
                    "The dictionary contains a candidate that violates the password rules.");
            }

            if (!uniqueCandidates.Add(candidate))
            {
                throw new InvalidOperationException(
                    "The dictionary contains duplicate candidates.");
            }
        }
    }

    private static bool IsValidCandidate(string? candidate)
    {
        if (candidate is null || candidate.Length != PasswordRules.CharacterOptions.Count)
        {
            return false;
        }

        for (var position = 0; position < candidate.Length; position++)
        {
            if (!PasswordRules.CharacterOptions[position].Contains(candidate[position]))
            {
                return false;
            }
        }

        return true;
    }
}
