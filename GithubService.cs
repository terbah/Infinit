using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Octokit;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GithubStats
{
    public class GitHubService
    {
        private readonly GitHubClient _client;
        private readonly SemaphoreSlim _semaphore;
        private readonly ILogger<GitHubService> _logger;
        private readonly string _owner;
        private readonly string _repo;
        private const int MaxConcurrency = 100;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public GitHubService(IConfiguration configuration, ILogger<GitHubService> logger)
        {
            _logger = logger;
            _client = new GitHubClient(new ProductHeaderValue("LodashStatsApp"))
            {
                Credentials = new Credentials(configuration["GitHub:Token"])
            };
            _owner = configuration["GitHub:Owner"];
            _repo = configuration["GitHub:Repo"];
            _semaphore = new SemaphoreSlim(MaxConcurrency);
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public async Task ProcessRepository(LetterCounter letterCounter)
        {
            try
            {
                await ProcessDirectory(letterCounter, _cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the repository.");
                _cancellationTokenSource.Cancel();
                throw;
            }
        }

        private async Task ProcessDirectory(LetterCounter letterCounter, CancellationToken token, string? path = null)
        {
            try
            {
                IReadOnlyList<RepositoryContent> contents = string.IsNullOrEmpty(path)
                    ? await _client.Repository.Content.GetAllContents(_owner, _repo)
                    : await _client.Repository.Content.GetAllContents(_owner, _repo, path);

                var tasks = contents.Select(async item =>
                {
                    if (item.Type == ContentType.Dir)
                    {
                        _logger.LogInformation("Entering directory: {DirectoryPath}", item.Path);
                        await ProcessDirectory(letterCounter, token, item.Path);
                    }
                    else if (item.Type == ContentType.File && (item.Name.EndsWith(".js") || item.Name.EndsWith(".ts")))
                    {
                        await ProcessFile(item, letterCounter, token);
                    }
                });

                await Task.WhenAll(tasks);
            }
            catch (RateLimitExceededException ex)
            {
                _logger.LogError("Rate limit exceeded. Stopping processing. Reset time: {ResetTime}", ex.Reset.UtcDateTime);
                _cancellationTokenSource.Cancel();
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Processing was canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while processing the directory {Path}", path);
                _cancellationTokenSource.Cancel();
                throw;
            }
        }

        private async Task ProcessFile(RepositoryContent item, LetterCounter letterCounter, CancellationToken token)
        {
            await _semaphore.WaitAsync(token);
            try
            {
                _logger.LogDebug("Processing file: {FileName}", item.Name);
                var fileContent = await _client.Repository.Content.GetAllContents(_owner, _repo, item.Path);
                var file = fileContent.First();

                if (file.Encoding == "base64")
                {
                    var content = file.Content;
                    if (!string.IsNullOrEmpty(content))
                    {
                        letterCounter.CountLetters(content);
                    }
                    else
                    {
                        _logger.LogWarning("File {FileName} is empty or could not be read.", item.Name);
                    }
                }
                else
                {
                    _logger.LogWarning("File {FileName} is not base64 encoded.", item.Name);
                }
            }
            catch (RateLimitExceededException ex)
            {
                _logger.LogError("Rate limit exceeded while processing file {FileName}. Reset time: {ResetTime}", item.Name, ex.Reset.UtcDateTime);
                _cancellationTokenSource.Cancel();
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Processing file {FileName} was canceled.", item.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while processing file {FileName}", item.Name);
                _cancellationTokenSource.Cancel();
                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
