using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using WordRecorder.Models;

namespace WordRecorder.Services;

public class YoudaoDictService
{
    private readonly HttpClient _httpClient = new();

    public async Task<Word> LookupWordAsync(string term)
    {
        var word = new Word { Term = term };

        try
        {
            var url = $"https://dict.youdao.com/jsonapi?q={Uri.EscapeDataString(term)}";
            var response = await _httpClient.GetStringAsync(url);
            using var doc = JsonDocument.Parse(response);
            var root = doc.RootElement;

            if (root.TryGetProperty("ec", out var ec))
            {
                if (ec.TryGetProperty("word", out var wordArr) && wordArr.GetArrayLength() > 0)
                {
                    var firstWord = wordArr[0];

                    if (firstWord.TryGetProperty("usphone", out var usphone))
                        word.Phonetic = $"[{usphone.GetString()}]";

                    if (firstWord.TryGetProperty("trs", out var trs) && trs.GetArrayLength() > 0)
                    {
                        var translations = new List<string>();
                        foreach (var tr in trs.EnumerateArray())
                        {
                            if (tr.TryGetProperty("tr", out var trArr) && trArr.GetArrayLength() > 0)
                            {
                                var trans = trArr[0];
                                if (trans.TryGetProperty("l", out var l))
                                {
                                    if (l.TryGetProperty("i", out var i) && i.GetArrayLength() > 0)
                                    {
                                        translations.Add(i[0].GetString() ?? "");
                                    }
                                }
                            }
                        }
                        word.Translation = string.Join("; ", translations);
                    }
                }
            }

            if (string.IsNullOrEmpty(word.Translation) && root.TryGetProperty("fanyi", out var fanyi))
            {
                if (fanyi.TryGetProperty("tran", out var tran))
                {
                    word.Translation = tran.GetString() ?? "";
                }
            }
        }
        catch
        {
            word.Translation = "(查询失败)";
        }

        return word;
    }

    /// <summary>
    /// 从有道 API 获取英文推荐词（同义词、相关词）
    /// 只返回英文单词/短语，过滤中文
    /// </summary>
    public async Task<List<string>> GetSuggestionsAsync(string input)
    {
        var suggestions = new List<string>();

        try
        {
            var url = $"https://dict.youdao.com/jsonapi?q={Uri.EscapeDataString(input)}";
            var response = await _httpClient.GetStringAsync(url);
            using var doc = JsonDocument.Parse(response);
            var root = doc.RootElement;

            // 1. 从 syno（同义词）提取 - 英文同义词
            if (root.TryGetProperty("syno", out var syno))
            {
                if (syno.TryGetProperty("synos", out var synos))
                {
                    foreach (var synoItem in synos.EnumerateArray())
                    {
                        if (synoItem.TryGetProperty("ws", out var ws))
                        {
                            foreach (var w in ws.EnumerateArray())
                            {
                                if (w.TryGetProperty("w", out var word))
                                {
                                    var s = word.GetString();
                                    if (IsEnglishWord(s) && !suggestions.Any(x => string.Equals(x, s, StringComparison.OrdinalIgnoreCase)))
                                        suggestions.Add(s!);
                                }
                            }
                        }
                    }
                }
            }

            // 2. 从 rel_word（相关词）提取 - 英文相关词
            if (root.TryGetProperty("rel_word", out var relWord))
            {
                if (relWord.TryGetProperty("rels", out var rels))
                {
                    foreach (var rel in rels.EnumerateArray())
                    {
                        if (rel.TryGetProperty("words", out var words))
                        {
                            foreach (var w in words.EnumerateArray())
                            {
                                if (w.TryGetProperty("word", out var word))
                                {
                                    var s = word.GetString();
                                    if (IsEnglishWord(s) && !suggestions.Any(x => string.Equals(x, s, StringComparison.OrdinalIgnoreCase)))
                                        suggestions.Add(s!);
                                }
                            }
                        }
                    }
                }
            }

            // 3. 从 phrs（短语）提取 - 英文短语
            if (root.TryGetProperty("phrs", out var phrs))
            {
                if (phrs.TryGetProperty("phrs", out var phrList))
                {
                    foreach (var phr in phrList.EnumerateArray())
                    {
                        if (phr.TryGetProperty("headword", out var headword))
                        {
                            var s = headword.GetString();
                            if (IsEnglishPhrase(s) && !suggestions.Any(x => string.Equals(x, s, StringComparison.OrdinalIgnoreCase)))
                                suggestions.Add(s!);
                        }
                    }
                }
            }
        }
        catch
        {
            // Ignore errors
        }

        return suggestions;
    }

    private static bool IsEnglishWord(string? s)
    {
        if (string.IsNullOrEmpty(s) || s.Length < 2 || s.Length > 40)
            return false;
        foreach (char c in s)
        {
            if (!((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '-' || c == '\''))
                return false;
        }
        return true;
    }

    private static bool IsEnglishPhrase(string? s)
    {
        if (string.IsNullOrEmpty(s) || s.Length < 3 || s.Length > 50)
            return false;
        foreach (char c in s)
        {
            if (!((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '-' || c == '\'' || c == ' '))
                return false;
        }
        return s.Any(char.IsLetter);
    }
}
