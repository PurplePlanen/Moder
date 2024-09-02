﻿using System.Collections.Frozen;
using Microsoft.Extensions.Logging;
using Moder.Core.Parser;
using Moder.Core.Services.Config;

namespace Moder.Core.Services;

public sealed class GameModDescriptorService
{
    public string Name { get; } = string.Empty;

    /// <summary>
    /// 保存着替换的文件夹相对路径的只读集合
    /// </summary>
    /// <remarks>
    /// 线程安全
    /// </remarks>
    public IReadOnlySet<string> ReplacePaths => _replacePaths;
    private readonly FrozenSet<string> _replacePaths;

    private const string FileName = "descriptor.mod";

    /// <summary>
    /// 按文件绝对路径构建
    /// </summary>
    /// <exception cref="FileNotFoundException">当文件不存在时</exception>
    /// <exception cref="IOException"></exception>
    public GameModDescriptorService(ILogger<GameModDescriptorService> logger, GlobalSettingService settingService)
    {
        var descriptorFilePath = Path.Combine(settingService.ModRootFolderPath, FileName);
        if (!File.Exists(descriptorFilePath))
        {
            _replacePaths = FrozenSet<string>.Empty;
            logger.LogWarning("mod 描述文件不存在");
            return;
        }

        var parser = new TextParser(settingService.ModRootFolderPath);
        if (parser.IsFailure)
        {
            _replacePaths = FrozenSet<string>.Empty;
            logger.LogWarning("Mod descriptor.mod file read is failure");
            return;
        }

        var replacePathList = new List<string>();
        var root = parser.GetResult();

        foreach (var item in root.Leaves)
        {
            switch (item.Key)
            {
                case "name":
                    Name = item.ValueText;
                    break;
                case "replace_path":
                    var parts = item.ValueText.Split('/');
                    replacePathList.Add(Path.Combine(parts));
                    break;
            }
        }
        _replacePaths = replacePathList.ToFrozenSet();
    }
}