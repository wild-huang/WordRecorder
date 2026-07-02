using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordRecorder.Services;

public class LingoesLd2Reader
{
    private byte[] _data = Array.Empty<byte>();

    public List<string> Words { get; } = new();
    public List<string> Definitions { get; } = new();

    public async Task<bool> ReadFileAsync(string filePath)
    {
        try
        {
            _data = await File.ReadAllBytesAsync(filePath);
            if (_data.Length < 0x60) return false;

            int offsetData = ReadInt(0x5C) + 0x60;
            if (_data.Length <= offsetData) return false;

            int type = ReadInt(offsetData);
            int offsetWithInfo = ReadInt(offsetData + 4) + offsetData + 12;

            int dictOffset = type == 3 ? offsetData : offsetWithInfo;
            if (type != 3 && _data.Length <= offsetWithInfo - 0x1C) return false;

            return ReadDictionary(dictOffset);
        }
        catch
        {
            return false;
        }
    }

    private bool ReadDictionary(int offsetWithIndex)
    {
        try
        {
            int limit = ReadInt(offsetWithIndex + 4) + offsetWithIndex + 8;
            int offsetIndex = offsetWithIndex + 0x1C;
            int offsetCompressedDataHeader = ReadInt(offsetWithIndex + 8) + offsetIndex;
            int inflatedWordsIndexLength = ReadInt(offsetWithIndex + 12);
            int definitions = (offsetCompressedDataHeader - offsetIndex) / 4;

            // Read deflate stream offsets
            int pos = offsetCompressedDataHeader + 8;
            ReadInt(pos); pos += 4;

            var deflateStreams = new List<int>();
            int currentOffset = 0;
            while (currentOffset + pos < limit)
            {
                currentOffset = ReadInt(pos);
                deflateStreams.Add(currentOffset);
                pos += 4;
            }

            int dataStart = pos;

            // Decompress all blocks
            using var output = new MemoryStream();
            int lastOffset = dataStart;
            for (int i = 0; i < deflateStreams.Count; i++)
            {
                int offset = dataStart + deflateStreams[i];
                int length = offset - lastOffset;
                if (length > 0 && lastOffset >= 0 && lastOffset + length <= _data.Length)
                {
                    try
                    {
                        byte[] compressed = new byte[length];
                        Array.Copy(_data, lastOffset, compressed, 0, length);
                        using var input = new MemoryStream(compressed);
                        using var zlib = new ZLibStream(input, CompressionMode.Decompress);
                        zlib.CopyTo(output);
                    }
                    catch { }
                }
                lastOffset = offset;
            }

            byte[] inflated = output.ToArray();
            if (inflated.Length == 0) return false;

            // Extract words with UTF-8 encoding
            for (int i = 0; i < definitions; i++)
            {
                int idxPos = 10 * i;
                if (idxPos + 14 > inflated.Length) break;

                int lastWordPos = BitConverter.ToInt32(inflated, idxPos);
                int currentWordOffset = BitConverter.ToInt32(inflated, idxPos + 10);
                int wordLen = currentWordOffset - lastWordPos;

                if (wordLen > 0 && lastWordPos >= 0 && inflatedWordsIndexLength + lastWordPos + wordLen <= inflated.Length)
                {
                    try
                    {
                        string word = Encoding.UTF8.GetString(inflated, inflatedWordsIndexLength + lastWordPos, wordLen);
                        word = CleanWord(word);
                        if (!string.IsNullOrEmpty(word) && word.Length >= 2 && word.Length <= 80)
                        {
                            Words.Add(word);
                        }
                    }
                    catch { }
                }
            }

            return Words.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    private string CleanWord(string word)
    {
        // Remove null bytes and control characters
        var sb = new StringBuilder(word.Length);
        foreach (char c in word)
        {
            if (c == '\0') continue;
            if (char.IsControl(c) && c != ' ' && c != '\t') continue;
            sb.Append(c);
        }
        return sb.ToString().Trim();
    }

    private int ReadInt(int index)
    {
        if (index + 4 > _data.Length) return 0;
        return BitConverter.ToInt32(_data, index);
    }
}
