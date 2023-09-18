using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Xabe.FFmpeg;

namespace VideoCompressor
{
    internal static class Compressor
    {
        internal static async Task CompressVideosParallel(List<string> inputFilePaths, string outputDirectory, Parameters param, MainWindow mainWindow)
        {
            var tasks = inputFilePaths.Select(async inputFilePath =>
            {
                await CompressVideo(inputFilePath, outputDirectory, param, mainWindow);
            });

            await Task.WhenAll(tasks);
        }

        internal static async Task CompressVideo(string inputFilePath, string outputDirectory, Parameters param, MainWindow mainWindow)
        {
            try
            {
                var mediaInfo = await FFmpeg.GetMediaInfo(inputFilePath).ConfigureAwait(true);
                var videoStreamInfo = mediaInfo.VideoStreams.FirstOrDefault();
                var audioStreamInfo = mediaInfo.AudioStreams.FirstOrDefault();

                if (videoStreamInfo == null)
                {
                    throw new Exception("No video stream found in the input file.");
                }

                var (height, width) = ParametersManager.CalculateVideoSize(videoStreamInfo.Height, videoStreamInfo.Width, param.resolution);
                var fps = Math.Min(25.0, videoStreamInfo.Framerate);

                var videoStream = ConfigureVideoStream(videoStreamInfo, param, height, width, fps);
                var audioStream = ConfigureAudioStream(audioStreamInfo);

                var outputFile = Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(inputFilePath) + "_compressed.mp4");

                var conversion = ConfigureConversion(videoStream, audioStream, outputFile, fps, param);

                ConfigureHardwareAcceleration(conversion, param);

                ConfigurePreset(conversion, param);

                ConfigureProgressCallback(conversion, mainWindow, inputFilePath);

                await conversion.Start();
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"Compressing error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static IStream ConfigureVideoStream(IVideoStream videoStreamInfo, Parameters param, int height, int width, double fps)
        {
            return videoStreamInfo
                .SetCodec(param.codec)
                .SetBitrate(Math.Min(ParametersManager.CalculateVideoBitrate(height, width, param.bitrateOption, fps), videoStreamInfo.Bitrate))
                .SetSize(height, width);
        }

        private static IStream ConfigureAudioStream(IAudioStream audioStreamInfo)
        {
            if (audioStreamInfo != null)
            {
                return audioStreamInfo
                    .SetCodec(AudioCodec.aac)
                    .SetBitrate(131_000);
            }
            return null;
        }

        private static IConversion ConfigureConversion(IStream videoStream, IStream audioStream, string outputFile, double fps, Parameters param)
        {
            return FFmpeg.Conversions.New()
                .AddStream(videoStream)
                .AddStream(audioStream)
                .SetOutput(outputFile)
                .SetFrameRate(fps)
                .UseMultiThread(true)
                .SetOverwriteOutput(true);
        }

        private static void ConfigureHardwareAcceleration(IConversion conversion, Parameters param)
        {
            if (param.codec == "hevc_qsv")
            {
                conversion.UseHardwareAcceleration(HardwareAccelerator.auto, VideoCodec.h264, VideoCodec.hevc_qsv);
            }
        }

        private static void ConfigurePreset(IConversion conversion, Parameters param)
        {
            if (IsPresetSupportedForCodec(param.codec, "veryslow"))
            {
                conversion.SetPreset(ConversionPreset.VerySlow);
            }
        }

        private static void ConfigureProgressCallback(IConversion conversion, MainWindow mainWindow, string inputFilePath)
        {
            conversion.OnProgress += async (sender, args) =>
            {
                var progress = args.Percent;
                await mainWindow.Dispatcher.InvokeAsync(() =>
                {
                    mainWindow.UpdateProgressUI(Path.GetFileNameWithoutExtension(inputFilePath), progress);
                }, System.Windows.Threading.DispatcherPriority.Normal);
            };
        }

        private static bool IsPresetSupportedForCodec(string codecName, string presetName)
        {
            string ffmpegPath = Path.Combine(FFmpeg.ExecutablesPath,"ffmpeg.exe");
            string codecInfoCommand = $"{ffmpegPath} -hide_banner -h encoder={codecName}";

            using (Process process = new Process())
            {
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.UseShellExecute = false;

                process.Start();

                StreamWriter sw = process.StandardInput;
                StreamReader sr = process.StandardOutput;

                sw.WriteLine(codecInfoCommand);
                sw.WriteLine("exit");

                string output = sr.ReadToEnd();

                if (output.Contains(presetName))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
