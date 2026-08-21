using WrapPassword.Application.Services;
using WrapPassword.Application.UseCases;
using WrapPassword.Domain.Passwords;
using WrapPassword.Infrastructure.Files;
using WrapPassword.Infrastructure.Packaging;
using WrapPassword.Infrastructure.RecruitmentApi;

const string UsernameEnvironmentVariable = "WRAP_PASSWORD_USERNAME";
const string DefaultUsername = "John";
const int HttpTimeoutSeconds = 15;

if (args.Length == 0 || args[0] is "--help" or "-h")
{
    PrintUsage();
    return 0;
}

using var cancellationSource = new CancellationTokenSource();

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellationSource.Cancel();
};

try
{
    return args[0].ToLowerInvariant() switch
    {
        "generate" => await GenerateDictionaryAsync(args, cancellationSource.Token),
        "prepare" => await PrepareArchiveAsync(args, cancellationSource.Token),
        "authenticate" => await AuthenticateAsync(args, cancellationSource.Token),
        _ => UnknownCommand(args[0])
    };
}
catch (OperationCanceledException) when (cancellationSource.IsCancellationRequested)
{
    Console.Error.WriteLine("Operation cancelled.");
    return 2;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"Operation failed: {exception.Message}");
    return 1;
}

static async Task<int> GenerateDictionaryAsync(
    string[] commandArguments,
    CancellationToken cancellationToken)
{
    if (commandArguments.Length > 2)
    {
        Console.Error.WriteLine("The generate command accepts at most one output path.");
        PrintUsage();
        return 1;
    }

    var outputPath = commandArguments.Length == 2 ? commandArguments[1] : "dict.txt";
    var generator = new PasswordDictionaryGenerator();
    var writer = new PasswordDictionaryFileWriter();
    var generateDictionary = new GeneratePasswordDictionaryUseCase(generator, writer);
    var result = await generateDictionary.ExecuteAsync(outputPath, cancellationToken);

    Console.WriteLine($"Generated {result.CandidateCount:N0} password candidates.");
    Console.WriteLine($"Dictionary: {result.OutputPath}");
    return 0;
}

static async Task<int> PrepareArchiveAsync(
    string[] commandArguments,
    CancellationToken cancellationToken)
{
    if (commandArguments.Length is < 2 or > 3)
    {
        Console.Error.WriteLine(
            "The prepare command requires a CV path and accepts one optional ZIP output path.");
        PrintUsage();
        return 1;
    }

    var cvPath = commandArguments[1];
    var outputPath = commandArguments.Length == 3
        ? commandArguments[2]
        : Path.Combine("artifacts", "submission.zip");
    var repositoryRoot = Directory.GetCurrentDirectory();

    var generator = new PasswordDictionaryGenerator();
    var writer = new PasswordDictionaryFileWriter();
    var generateDictionary = new GeneratePasswordDictionaryUseCase(generator, writer);
    var archiveBuilder = new SubmissionArchiveBuilder();
    var prepareArchive = new PrepareSubmissionArchiveUseCase(
        generateDictionary,
        archiveBuilder);

    Console.WriteLine("Preparing the submission ZIP locally. No network requests will be made.");

    var result = await prepareArchive.ExecuteAsync(
        repositoryRoot,
        cvPath,
        outputPath,
        cancellationToken);

    Console.WriteLine($"Archive: {result.ArchivePath}");
    Console.WriteLine($"Files: {result.Entries.Count:N0}");
    Console.WriteLine($"Size: {result.SizeInBytes:N0} bytes");
    Console.WriteLine($"SHA-256: {result.Sha256}");
    return 0;
}

static async Task<int> AuthenticateAsync(
    string[] commandArguments,
    CancellationToken cancellationToken)
{
    if (commandArguments.Length != 1)
    {
        Console.Error.WriteLine("The authenticate command does not accept arguments.");
        PrintUsage();
        return 1;
    }

    var username = Environment.GetEnvironmentVariable(UsernameEnvironmentVariable)
        ?? DefaultUsername;

    using var handler = new SocketsHttpHandler
    {
        AllowAutoRedirect = false
    };
    using var httpClient = new HttpClient(handler)
    {
        Timeout = TimeSpan.FromSeconds(HttpTimeoutSeconds)
    };

    var generator = new PasswordDictionaryGenerator();
    using var authenticationClient = new RecruitmentAuthenticationClient(httpClient);
    var authenticateCandidates = new AuthenticatePasswordCandidatesUseCase(
        generator,
        authenticationClient);

    Console.WriteLine(
        $"Trying {PasswordRules.ExpectedCandidateCount:N0} candidates at "
        + $"{RecruitmentAuthenticationClient.DefaultRequestsPerSecond} requests per second.");
    Console.WriteLine("Press Ctrl+C to cancel. Credentials and URLs will not be displayed.");

    var result = await authenticateCandidates.ExecuteAsync(username, cancellationToken);

    Console.WriteLine($"Authentication succeeded after {result.AttemptCount:N0} attempts.");
    Console.WriteLine("The temporary upload URL was received and validated.");
    return 0;
}

static int UnknownCommand(string command)
{
    Console.Error.WriteLine($"Unknown command: {command}");
    PrintUsage();
    return 1;
}

static void PrintUsage()
{
    Console.WriteLine("Wrap Password Assessment");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run --project src/WrapPassword.Cli -- generate [output-path]");
    Console.WriteLine("  dotnet run --project src/WrapPassword.Cli -- prepare <cv-path> [output-path]");
    Console.WriteLine("  dotnet run --project src/WrapPassword.Cli -- authenticate");
}
