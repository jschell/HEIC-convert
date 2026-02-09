using System.Windows;
using HEICAutoConverter.Core;
using Microsoft.Win32;
using FolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;
using DialogResult = System.Windows.Forms.DialogResult;
using MessageBox = System.Windows.MessageBox;

namespace HEICAutoConverter.UI;

public partial class SettingsWindow : Window
{
    private readonly Settings _settings;
    public bool SettingsSaved { get; private set; }

    public SettingsWindow(Settings settings)
    {
        InitializeComponent();
        _settings = settings.Clone();
        LoadSettings();
    }

    private void LoadSettings()
    {
        // Watch folders
        FolderList.Items.Clear();
        foreach (var folder in _settings.WatchFolders)
            FolderList.Items.Add(folder);

        IncludeSubdirs.IsChecked = _settings.IncludeSubdirectories;

        // Output strategy
        switch (_settings.OutputStrategy)
        {
            case OutputStrategy.CustomFolder:
                OutputCustomFolder.IsChecked = true;
                break;
            case OutputStrategy.MirrorStructure:
                OutputMirror.IsChecked = true;
                break;
            default:
                OutputSameFolder.IsChecked = true;
                break;
        }
        CustomOutputPath.Text = _settings.CustomOutputFolder;

        // File action
        switch (_settings.OriginalFileAction)
        {
            case OriginalFileAction.Delete:
                ActionDelete.IsChecked = true;
                break;
            case OriginalFileAction.MoveToArchive:
                ActionArchive.IsChecked = true;
                break;
            default:
                ActionKeep.IsChecked = true;
                break;
        }
        ArchivePath.Text = _settings.ArchiveFolder;

        // Conversion
        QualitySlider.Value = _settings.JpegQuality;
        QualityLabel.Text = _settings.JpegQuality.ToString();
        ConcurrencySlider.Value = _settings.MaxConcurrentConversions;
        ConcurrencyLabel.Text = _settings.MaxConcurrentConversions.ToString();
        SkipExisting.IsChecked = _settings.SkipExisting;

        // Behavior
        StartWithWindows.IsChecked = _settings.StartWithWindows;
        StartMinimized.IsChecked = _settings.StartMinimized;
        ShowNotifications.IsChecked = _settings.ShowNotifications;
    }

    private void OnAddFolder(object sender, RoutedEventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select a folder to watch for HEIC files",
            UseDescriptionForTitle = true
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            if (!FolderList.Items.Contains(dialog.SelectedPath))
                FolderList.Items.Add(dialog.SelectedPath);
        }
    }

    private void OnRemoveFolder(object sender, RoutedEventArgs e)
    {
        if (FolderList.SelectedItem != null)
            FolderList.Items.Remove(FolderList.SelectedItem);
    }

    private void OnBrowseOutputFolder(object sender, RoutedEventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select output folder for converted JPG files",
            UseDescriptionForTitle = true
        };

        if (dialog.ShowDialog() == DialogResult.OK)
            CustomOutputPath.Text = dialog.SelectedPath;
    }

    private void OnBrowseArchiveFolder(object sender, RoutedEventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select archive folder for original HEIC files",
            UseDescriptionForTitle = true
        };

        if (dialog.ShowDialog() == DialogResult.OK)
            ArchivePath.Text = dialog.SelectedPath;
    }

    private void OnOutputStrategyChanged(object sender, RoutedEventArgs e)
    {
        if (CustomOutputPath == null) return;
        CustomOutputPath.IsEnabled = OutputCustomFolder.IsChecked == true || OutputMirror.IsChecked == true;
    }

    private void OnQualityChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (QualityLabel != null)
            QualityLabel.Text = ((int)e.NewValue).ToString();
    }

    private void OnConcurrencyChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ConcurrencyLabel != null)
            ConcurrencyLabel.Text = ((int)e.NewValue).ToString();
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        // Validate
        if (FolderList.Items.Count == 0)
        {
            MessageBox.Show("Please add at least one watch folder.", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if ((OutputCustomFolder.IsChecked == true || OutputMirror.IsChecked == true)
            && string.IsNullOrWhiteSpace(CustomOutputPath.Text))
        {
            MessageBox.Show("Please specify a custom output folder.", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (ActionArchive.IsChecked == true && string.IsNullOrWhiteSpace(ArchivePath.Text))
        {
            MessageBox.Show("Please specify an archive folder.", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Save to settings object
        _settings.WatchFolders = FolderList.Items.Cast<string>().ToList();
        _settings.IncludeSubdirectories = IncludeSubdirs.IsChecked == true;

        _settings.OutputStrategy = OutputCustomFolder.IsChecked == true ? OutputStrategy.CustomFolder
            : OutputMirror.IsChecked == true ? OutputStrategy.MirrorStructure
            : OutputStrategy.SameFolder;
        _settings.CustomOutputFolder = CustomOutputPath.Text;

        _settings.OriginalFileAction = ActionDelete.IsChecked == true ? OriginalFileAction.Delete
            : ActionArchive.IsChecked == true ? OriginalFileAction.MoveToArchive
            : OriginalFileAction.Keep;
        _settings.ArchiveFolder = ArchivePath.Text;

        _settings.JpegQuality = (int)QualitySlider.Value;
        _settings.MaxConcurrentConversions = (int)ConcurrencySlider.Value;
        _settings.SkipExisting = SkipExisting.IsChecked == true;

        _settings.StartWithWindows = StartWithWindows.IsChecked == true;
        _settings.StartMinimized = StartMinimized.IsChecked == true;
        _settings.ShowNotifications = ShowNotifications.IsChecked == true;

        _settings.Save();
        _settings.UpdateStartWithWindows();

        SettingsSaved = true;
        DialogResult = true;
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    public Settings GetSettings() => _settings;
}
