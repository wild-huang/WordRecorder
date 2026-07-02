# 写在前面

>这个应用是使用ai制作的，所以说ai味道会特别重。bro网上找不到一个简单好用的生词本应用(也许是伪需求?)所以烧了将近10块钱的token把这鬼东西搞出来了


# 📖 生词记录 (WordRecorder)

一款简洁的生词记录工具，支持有道词典查询、AI 猜词、多格式导出。

![.NET 8](https://img.shields.io/badge/.NET-8-purple) ![Avalonia](https://img.shields.io/badge/Avalonia-11.2-blue) ![License](https://img.shields.io/badge/License-MIT-green)

## ✨ 功能特性

- **云母(Mica)背景** - Windows 11 原生毛玻璃效果
- **简洁输入体验** - 按回车添加单词，自动清空输入框
- **实时指示器** - 输入时即时显示单词是否存在于词典（绿点/红点）
- **智能推荐词** - 本地词典前缀匹配 + AI 纠错补全
- **有道词典查询** - 自动获取音标和中文释义
- **重复检测** - 已查单词自动置顶并显示查询次数
- **日期分组** - 按添加日期自动分组显示
- **多格式导出** - 支持 txt、xlsx、docx、pdf、png
- **主题定制** - 浅色/深色/跟随系统 + 晚霞配色 + 自定义调色板
- **灵格斯词典** - 支持导入 ld2/ldx 格式本地词典

## 📸 截图

<!-- TODO: 添加截图 -->

## 🚀 快速开始

### 环境要求

- Windows 10/11
- .NET 8.0 Desktop Runtime

### 安装

1. 从 [Releases](../../releases) 下载最新版本
2. 解压后运行 `WordRecorder.exe`

### 从源码构建

```bash
git clone https://github.com/wild-huang/WordRecorder.git
cd WordRecorder
dotnet run
```

## 📖 使用说明

### 基本操作

1. 在输入框输入英文单词，按回车添加
2. 系统自动查询有道词典获取释义
3. 输入框右侧指示器显示单词是否存在（绿点/红点）
4. 输入 4 个字母以上时，AI 会自动推荐可能的单词

### 导入词典

1. 点击右上角"⚙ 设置"
2. 在"词典设置"中点击"浏览"选择词典文件
3. 支持格式：`.ld2`、`.ldx`、`.txt`、`.csv`

### 导出生词本

1. 点击右上角"📤 导出"
2. 选择保存格式和位置
3. 支持格式：txt、xlsx、docx、pdf、png

## ⚙️ 配置

### AI 设置

在设置中配置 OpenAI 兼容的 API：

| 配置项 | 说明 |
|--------|------|
| API 端点 | OpenAI 兼容接口地址 |
| API 密钥 | 你的 API Key |
| 模型名称 | 如 `gpt-3.5-turbo`、`deepseek-v4-flash` |

推荐使用 [DeepSeek](https://platform.deepseek.com/)，成本极低。

## 🛠️ 技术栈

- **框架**: Avalonia UI 11.2
- **MVVM**: CommunityToolkit.Mvvm
- **导出**: ClosedXML (Excel)、OpenXml (Word)、QuestPDF (PDF)
- **词典**: 灵格斯 LD2 解析器
- **API**: 有道词典、OpenAI 兼容接口

## 📁 项目结构

```
WordRecorder/
├── Models/           # 数据模型
│   ├── Word.cs       # 单词模型
│   └── AppSettings.cs # 应用设置
├── Services/         # 服务层
│   ├── YoudaoDictService.cs    # 有道词典服务
│   ├── AiService.cs            # AI 推荐服务
│   ├── LingoesDictService.cs   # 灵格斯词典服务
│   ├── LingoesLd2Reader.cs     # LD2 文件解析器
│   ├── ExportService.cs        # 导出服务
│   └── SettingsService.cs      # 设置持久化
├── ViewModels/       # 视图模型
│   └── MainWindowViewModel.cs
├── Views/            # 视图
│   ├── MainWindow.axaml
│   └── MainWindow.axaml.cs
└── Converters/       # 值转换器
```

## 📄 许可证

本项目采用 [MIT 许可证](LICENSE)。

## 🙏 致谢

- [Avalonia UI](https://avaloniaui.net/) - 跨平台 .NET UI 框架
- [有道词典](https://www.youdao.com/) - 词典查询 API
- [灵格斯词霸](http://www.lingoes.cn/) - 词典文件格式参考
- [kdictionary-lingoes](https://github.com/librehat/kdictionary-lingoes) - LD2 解析参考


## Powered by OpenCode&XiaomiMIMO
