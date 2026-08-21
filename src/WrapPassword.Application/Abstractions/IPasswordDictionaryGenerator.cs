namespace WrapPassword.Application.Abstractions;

public interface IPasswordDictionaryGenerator
{
    IEnumerable<string> Generate();
}
