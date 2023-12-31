﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoCompressor
{
    public class FileInfoItem : INotifyPropertyChanged
    {
        private string fileName;
        public string FileName
        {
            get { return fileName; }
            set
            {
                fileName = value;
                OnPropertyChanged(nameof(FileName));
            }
        }

        private string fileSize;
        public string FileSize
        {
            get { return fileSize; }
            set
            {
                fileSize = value;
                OnPropertyChanged(nameof(FileSize));
            }
        }

        private double compressionProgress;
        public double CompressionProgress
        {
            get { return compressionProgress; }
            set
            {
                compressionProgress = value;
                OnPropertyChanged(nameof(CompressionProgress));
            }
        }

        private TimeSpan remainingTime;

        public string ExpectedTimeFormatted
        {
            get { return remainingTime.ToString(@"hh\:mm\:ss"); }
        }

        public TimeSpan RemainingTime
        {
            get { return remainingTime; }
            set
            {
                remainingTime = value;
                OnPropertyChanged(nameof(RemainingTime));
                OnPropertyChanged(nameof(ExpectedTimeFormatted));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
