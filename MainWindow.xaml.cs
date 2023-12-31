﻿// Ignore Spelling: codec

using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Xabe.FFmpeg;

namespace VideoCompressor
{

    public partial class MainWindow : Window
    {
        private readonly List<string> selectedFiles = new List<string>();
        private readonly string projectFolder = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);
        private readonly DispatcherTimer timer = new DispatcherTimer();
        private DateTime startTime;

        public MainWindow()
        {
            Initialize();
        }

        private void Initialize()
        {
            InitializeComponent();
            FFmpeg.SetExecutablesPath(Path.Combine(projectFolder, "ffmpeg", "bin"));

            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;

            Closing += MainWindow_Closing;
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            selectedFiles.Clear();
            SelectedFilesListView.Items.Clear();
            var openFileDialog = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "Video files (*.mp4, *.avi)|*.mp4;*.avi|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string selectedFile in openFileDialog.FileNames)
                {
                    string fileName = Path.GetFileNameWithoutExtension(selectedFile);
                    selectedFiles.Add(selectedFile);
                    SelectedFilesListView.Items.Add(new FileInfoItem
                    {
                        FileName = fileName,
                        RemainingTime=TimeSpan.Zero,
                        CompressionProgress = 0
                    });
                }
            }
        }

        private async void CompressButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (selectedFiles.Count == 0)
                {
                    ShowErrorMessage("Select the video files to be compressed");
                    CompressButton.IsEnabled = true;
                    StopButton.IsEnabled = false;
                    return;
                }

                var selectedPath = GetFolderPathForSaving();
                if (selectedPath != null)
                {
                    CompressButton.IsEnabled = false;
                    startTime = DateTime.Now;
                    timer.Start();
                    StopButton.IsEnabled = true;
                    await CompressVideos(selectedPath);
                }

            }
            catch (Exception ex)
            {
                HandleError($"Video compression error: {ex.Message}");
            }
            finally
            {
                StopButton.IsEnabled = false;
                timer.Stop();
                CompressButton.IsEnabled = true;
            }
        }

        private string GetFolderPathForSaving()
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Select a folder to save the file to"
            };

            return dialog.ShowDialog() == CommonFileDialogResult.Ok ? dialog.FileName : null;
        }

        private async Task CompressVideos(string selectedPath)
        {
            await Compressor.CompressVideosParallel(selectedFiles, selectedPath, GetParameters(), this);
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            timer.Stop();
            StopCompression();
            StopButton.IsEnabled = false;
        }

        internal void UpdateProgressUI(string fileName, double progress, TimeSpan elapsed)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var item = SelectedFilesListView.Items.Cast<FileInfoItem>()
                    .FirstOrDefault(x => x.FileName == fileName);

                if (item != null)
                {
                    double elapsedSeconds = elapsed.TotalSeconds;
                    double remainingTimeSeconds = (elapsedSeconds *100/progress)- elapsedSeconds;
                    TimeSpan remainingTime = TimeSpan.FromSeconds(remainingTimeSeconds);
                    item.RemainingTime = remainingTime;
                    item.CompressionProgress = progress;
                }
            });
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            TimeSpan elapsed = DateTime.Now - startTime;
            TimerTextBlock.Text = "Execution Time: " + elapsed.ToString(@"hh\:mm\:ss");
        }

        private Parameters GetParameters()
        {
            CultureInfo cultureInfo = CultureInfo.InvariantCulture;
            if (CodecComboBox.SelectedItem is ComboBoxItem codecItem && codecItem.Tag != null &&
                ResolutionComboBox.SelectedItem is ComboBoxItem resolutionItem && resolutionItem.Tag != null &&
                int.TryParse(resolutionItem.Tag.ToString(), out int res) &&
                BitrateComboBox.SelectedItem is ComboBoxItem bitrateItem && bitrateItem.Tag != null &&
                float.TryParse(bitrateItem.Tag.ToString(), NumberStyles.Float, cultureInfo, out float bitrate))
            {

                return new Parameters
                {
                    codec = codecItem.Tag.ToString(),
                    resolution = res,
                    bitrateOption = bitrate
                };
            }
            else
            {
                Console.WriteLine("false");
                return new Parameters
                {
                    codec = "h264",
                    resolution = 0,
                    bitrateOption = 0.1
                };
            }

        }

        private void StopCompression()
        {
            foreach (var process in Process.GetProcessesByName("ffmpeg"))
            {
                if (!process.CloseMainWindow())
                {
                    process.Kill();
                }
            }
            selectedFiles.Clear();
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            StopCompression();
        }

        private void ShowErrorMessage(string message)
        {
            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void HandleError(string errorMessage)
        {
            Debug.WriteLine(errorMessage);
            ShowErrorMessage(errorMessage);
        }
    }
}
