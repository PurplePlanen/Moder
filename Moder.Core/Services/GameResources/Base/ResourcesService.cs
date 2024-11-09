using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Moder.Core.Services.Config;
using NLog;

namespace Moder.Core.Services.GameResources.Base;

public abstract partial class ResourcesService<TType, TContent, TParseResult> : IResourcesService
    where TType : ResourcesService<TType, TContent, TParseResult>
{
    public readonly string FolderRelativePath;
    public event EventHandler<ResourceChangedEventArgs>? OnResourceChanged;

    /// <summary>
    /// key: 文件路径, value: 文件内资源内容
    /// </summary>
    protected readonly Dictionary<string, TContent> Resources;
    protected readonly Logger Logger;

    private readonly GlobalSettingService _settingService;
    private readonly string ServiceName = typeof(TType).Name;

    protected ResourcesService(string folderRelativePath, WatcherFilter filter)
    {
        FolderRelativePath = folderRelativePath;
        Logger = LogManager.GetLogger(typeof(TType).FullName);
        _settingService = App.Current.Services.GetRequiredService<GlobalSettingService>();
        var gameResourcesPathService = App.Current.Services.GetRequiredService<GameResourcesPathService>();
        var watcherService = App.Current.Services.GetRequiredService<GameResourcesWatcherService>();

        var filePaths = gameResourcesPathService.GetAllFilePriorModByRelativePathForFolder(
            FolderRelativePath
        );

        // Resources 必须在使用 ParseFileAndAddToResources 之前初始化
        Resources = new Dictionary<string, TContent>(filePaths.Count);

        foreach (var filePath in filePaths)
        {
            ParseFileAndAddToResources(filePath);
        }

        watcherService.Watch(FolderRelativePath, this, filter.Name);
        Logger.Info("初始化资源成功: {FolderRelativePath}, 共 {Count} 个文件", FolderRelativePath, filePaths.Count);
        LogItemsSum();
    }

    [Conditional("DEBUG")]
    private void LogItemsSum()
    {
        if (typeof(IReadOnlyCollection<object>).IsAssignableFrom(typeof(TContent)))
        {
            Logger.Debug(
                "'{Path}'下资源数量: {Count}",
                FolderRelativePath,
                Resources.Values.Cast<IReadOnlyCollection<object>>().Sum(content => content.Count)
            );
        }
    }

    void IResourcesService.Add(string folderOrFilePath)
    {
        Logger.Debug("添加 Mod 资源: {FolderOrFilePath}", folderOrFilePath);
        Debug.Assert(File.Exists(folderOrFilePath), "必须为文件");

        // 如果新增加的mod资源在原版资源中存在, 移除原版资源, 添加mod资源
        var relativeFilePath = Path.GetRelativePath(_settingService.ModRootFolderPath, folderOrFilePath);
        var gameFilePath = Path.Combine(_settingService.GameRootFolderPath, relativeFilePath);
        var isRemoved = Resources.Remove(gameFilePath);
        if (isRemoved)
        {
            Logger.Info("移除游戏资源成功: {GameFilePath}", gameFilePath);
        }

        ParseFileAndAddToResources(folderOrFilePath);
        OnOnResourceChanged(new ResourceChangedEventArgs(folderOrFilePath));

        Logger.Info("添加 Mod 资源成功: {FolderOrFilePath}", folderOrFilePath);
    }

    void IResourcesService.Remove(string folderOrFilePath)
    {
        Logger.Debug("移除 Mod 资源: {FolderOrFilePath}", folderOrFilePath);
        if (Directory.Exists(folderOrFilePath))
        {
            foreach (
                var filePath in Directory.GetFileSystemEntries(
                    folderOrFilePath,
                    "*",
                    SearchOption.AllDirectories
                )
            )
            {
                ((IResourcesService)this).Remove(filePath);
            }
        }

        if (Resources.Remove(folderOrFilePath))
        {
            Logger.Info("移除 Mod 资源成功");
            var relativeFilePath = Path.GetRelativePath(_settingService.ModRootFolderPath, folderOrFilePath);

            // 如果删除的mod资源在原版资源中存在, 移除mod资源, 添加原版资源
            var gameFilePath = Path.Combine(_settingService.GameRootFolderPath, relativeFilePath);
            if (!File.Exists(gameFilePath))
            {
                return;
            }

            ParseFileAndAddToResources(gameFilePath);
            OnOnResourceChanged(new ResourceChangedEventArgs(folderOrFilePath));

            Logger.Info("添加原版游戏资源: {GameFilePath}", gameFilePath);
        }
    }

    void IResourcesService.Reload(string folderOrFilePath)
    {
        Logger.Debug("尝试重新加载 Mod 资源: {FolderOrFilePath}", folderOrFilePath);
        if (Directory.Exists(folderOrFilePath))
        {
            Logger.Debug("跳过文件夹");
            return;
        }

        var isRemoved = Resources.Remove(folderOrFilePath);
        var isAdded = ParseFileAndAddToResources(folderOrFilePath);
        if (!isAdded)
        {
            Logger.Info("{ServiceName} 不加载此 Mod 资源", ServiceName);
            return;
        }

        // 当有移除或有添加时才触发事件
        if (isRemoved || isAdded)
        {
            OnOnResourceChanged(new ResourceChangedEventArgs(folderOrFilePath));
        }
        Logger.Info("{ServiceName} 重新加载 Mod 资源成功", ServiceName);
    }

    void IResourcesService.Renamed(string oldPath, string newPath)
    {
        Logger.Debug("Mod 资源重命名: {OldPath} -> {NewPath}", oldPath, newPath);
        if (Directory.Exists(newPath))
        {
            Logger.Debug("跳过文件夹");
            return;
        }

        if (Resources.TryGetValue(oldPath, out var countryTags))
        {
            Resources.Add(newPath, countryTags);
        }
        else
        {
            Logger.Debug("{ServiceName} 跳过处理 {NewPath} 重命名", GetType().Name, newPath);
            return;
        }
        Resources.Remove(oldPath);

        Logger.Info("Mod 资源重命名成功");
    }

    /// <summary>
    /// 解析 folderRelativePath 目录下的所有文件, 并将解析结果添加到 <see cref="Resources"/> 中
    /// </summary>
    /// <param name="result">文件解析结果</param>
    /// <returns>文件内资源内容, 当为 <c>null</c> 时表示该服务不对此文件的变化做出响应</returns>
    protected abstract TContent? ParseFileToContent(TParseResult result);

    protected abstract TParseResult? GetParseResult(string filePath);

    /// <summary>
    /// 解析文件, 并将解析结果添加到 <see cref="Resources"/> 中
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns>成功添加返回 <c>true</c>, 否则返回 <c>false</c></returns>
    private bool ParseFileAndAddToResources(string filePath)
    {
        var result = GetParseResult(filePath);
        return AddToResources(filePath, result);
    }

    private bool AddToResources(string filePath, TParseResult? result)
    {
        if (result is null)
        {
            Logger.Warn("文件 {FilePath} 解析失败", filePath);
            return false;
        }

        var content = ParseFileToContent(result);
        if (content is null)
        {
            return false;
        }

        Resources.Add(filePath, content);
        return true;
    }

    private void OnOnResourceChanged(ResourceChangedEventArgs e)
    {
        OnResourceChanged?.Invoke(this, e);
    }
}
