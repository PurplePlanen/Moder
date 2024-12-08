﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using EnumsNET;
using Microsoft.Extensions.DependencyInjection;
using Moder.Core.Converters;
using Moder.Core.Extensions;
using Moder.Core.Resources;
using Moder.Core.ViewsModel;
using AppInitializeControlViewModel = Moder.Core.ViewsModel.Menus.AppInitializeControlViewModel;

namespace Moder.Core.Views.Menus;

public partial class AppInitializeControlView : UserControl
{
    private IDisposable? _selectFolderInteractionDisposable;

    public AppInitializeControlView()
    {
        InitializeComponent();

        var viewModel = App.Services.GetRequiredService<AppInitializeControlViewModel>();
        DataContext = viewModel;
        ThemeSelector.SelectionChanged += ThemeSelectorOnSelectionChanged;
    }

    private void ThemeSelectorOnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var type = typeof(ThemeMode);
        var names = Enums.GetNames(type);
        var index = ThemeSelector.SelectedIndex;
        if (index >= names.Count || index < 0)
        {
            return;
        }
        var obj = names[index].ToEnum(type);
        if (obj is not ThemeMode theme)
        {
            return;
        }
        var app = Application.Current;
        if (app is null)
        {
            return;
        }
        app.RequestedThemeVariant = AppTheme.GetThemeVariant(theme);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        _selectFolderInteractionDisposable?.Dispose();

        if (DataContext is AppInitializeControlViewModel viewModel)
        {
            _selectFolderInteractionDisposable = viewModel.SelectFolderInteraction.RegisterHandler(Handler);
        }

        base.OnDataContextChanged(e);
    }

    private async Task<string> Handler(string title)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            return string.Empty;
        }

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions { Title = title, AllowMultiple = false }
        );
        var result = folders.Count > 0 ? folders[0].TryGetLocalPath() ?? string.Empty : string.Empty;

        return result;
    }
}
