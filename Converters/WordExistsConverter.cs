using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace WordRecorder.Converters;

public class WordExistsConverter : IMultiValueConverter
{
    public static WordExistsConverter Instance { get; } = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2)
            return new SolidColorBrush(Colors.Transparent);

        var wordExists = values[0] as bool?;
        var currentInput = values[1] as string;

        // 如果输入为空或太短，不显示指示器
        if (string.IsNullOrEmpty(currentInput) || currentInput.Length < 2)
            return new SolidColorBrush(Colors.Transparent);

        // 根据单词是否存在返回颜色
        if (wordExists == true)
            return new SolidColorBrush(Color.Parse("#4CAF50")); // 绿色
        else if (wordExists == false)
            return new SolidColorBrush(Color.Parse("#F44336")); // 红色
        else
            return new SolidColorBrush(Color.Parse("#9E9E9E")); // 灰色（检查中）
    }
}
