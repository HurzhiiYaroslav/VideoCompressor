using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Xabe.FFmpeg;

namespace VideoCompressor
{
    public static class ParametersManager
    {
        public static long CalculateVideoBitrate(int height, int width, double bitrateCoefficient, double frameRate)
        {
            if (frameRate <= 0)
            {
                throw new ArgumentException("Frame rate must be greater than zero.", nameof(frameRate));
            }
            Console.WriteLine((long)(height * width * bitrateCoefficient * frameRate/2));
            return (long)(height * width * bitrateCoefficient * frameRate/2);
        }

        public static (int, int) CalculateVideoSize(int height, int width, int targetResolution)
        {
            if (targetResolution <= 0 || height==targetResolution || width==targetResolution)
            {
                return (width,height);
            }

            if (height > width)
            {
                return (targetResolution,(int)(height * ((double)targetResolution / width)));
            }
            else
            {
                return ((int)(width * ((double)targetResolution / height)), targetResolution);
            }
        }
    }
}
