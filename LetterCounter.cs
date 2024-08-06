using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace GithubStats
{

    public class LetterCounter
    {
        private readonly ConcurrentDictionary<char, int> _letterCounts = new();
        private readonly ILogger<LetterCounter> _logger;

        public LetterCounter(ILogger<LetterCounter> logger)
        {
            _logger = logger;
        }

        public void CountLetters(string content)
        {
            foreach (var lowerC in from c in content
                                   where char.IsLetter(c)
                                   let lowerC = char.ToLower(c)
                                   select lowerC)
            {
                _letterCounts.AddOrUpdate(lowerC, 1, (key, oldValue) => oldValue + 1);
            }
        }

        public void DisplayResults()
        {
            var sortedCounts = _letterCounts.OrderByDescending(kv => kv.Value);

            _logger.LogInformation("Letter frequencies in JavaScript/TypeScript files:");
            int rank = 1;
            foreach (var kv in sortedCounts)
            {
                _logger.LogInformation("Rank {Rank}: {Letter} - {Count}", rank, kv.Key, kv.Value);
                rank++;
            }
        }
    }

}
