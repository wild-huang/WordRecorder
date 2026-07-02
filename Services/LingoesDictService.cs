using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordRecorder.Services;

public class LingoesDictService
{
    // Everything 风格：预排序数组 + HashSet 双索引
    private readonly HashSet<string> _hashSet = new(StringComparer.OrdinalIgnoreCase);
    private string[] _sorted = Array.Empty<string>();
    private readonly LingoesLd2Reader _ld2Reader = new();
    private bool _isLoaded;

    public bool IsLoaded => _isLoaded;
    public int WordCount => _hashSet.Count;

    public async Task ImportFromFileAsync(string filePath)
    {
        try
        {
            var ext = Path.GetExtension(filePath).ToLower();
            if (ext == ".txt" || ext == ".csv")
                await ImportFromTextAsync(filePath);
            else if (ext == ".ld2" || ext == ".ldx")
                await ImportFromLd2Async(filePath);
            else
                throw new NotSupportedException($"不支持的文件格式: {ext}");

            // 构建排序索引（一次性）
            _sorted = _hashSet.ToArray();
            Array.Sort(_sorted, StringComparer.OrdinalIgnoreCase);

            _isLoaded = _hashSet.Count > 0;
        }
        catch (Exception ex)
        {
            throw new Exception($"导入词典失败: {ex.Message}", ex);
        }
    }

    private async Task ImportFromTextAsync(string filePath)
    {
        var lines = await File.ReadAllLinesAsync(filePath);
        foreach (var line in lines)
        {
            var word = line.Trim().Split('\t', ',', '|', ' ')[0].Trim();
            if (!string.IsNullOrEmpty(word) && word.Length >= 2)
                _hashSet.Add(word.ToLowerInvariant());
        }
    }

    private async Task ImportFromLd2Async(string filePath)
    {
        bool success = await _ld2Reader.ReadFileAsync(filePath);
        if (success && _ld2Reader.Words.Count > 0)
        {
            foreach (var word in _ld2Reader.Words)
            {
                if (!string.IsNullOrWhiteSpace(word))
                {
                    var clean = word.Trim().ToLowerInvariant();
                    if (clean.Length >= 2 && clean.Length <= 80)
                        _hashSet.Add(clean);
                }
            }
        }
    }

    /// <summary>
    /// Everything 风格：O(1) HashSet 精确匹配
    /// </summary>
    public bool ContainsWord(string word)
    {
        return _isLoaded && !string.IsNullOrEmpty(word) && _hashSet.Contains(word.ToLowerInvariant());
    }

    /// <summary>
    /// Everything 风格：二分查找前缀匹配 + 向后扫描
    /// 已排序数组 → 二分定位第一个匹配 → 线性扫描收集结果
    /// </summary>
    public List<string> FindSimilar(string input, int maxResults = 5)
    {
        if (!_isLoaded || string.IsNullOrEmpty(input))
            return new List<string>();

        var lower = input.ToLowerInvariant();
        var results = new List<string>(maxResults);

        // 二分查找第一个 >= lower 的位置
        int lo = 0, hi = _sorted.Length;
        while (lo < hi)
        {
            int mid = (lo + hi) / 2;
            if (string.Compare(_sorted[mid], lower, StringComparison.OrdinalIgnoreCase) < 0)
                lo = mid + 1;
            else
                hi = mid;
        }

        // 从 lo 开始向后扫描，收集前缀匹配
        for (int i = lo; i < _sorted.Length && results.Count < maxResults; i++)
        {
            if (_sorted[i].StartsWith(lower, StringComparison.OrdinalIgnoreCase))
                results.Add(_sorted[i]);
            else
                break; // 已排序，后面的不可能匹配
        }

        return results;
    }
}
