using Microsoft.VisualStudio.TestTools.UnitTesting;
using VideoCompressor;
using System;

namespace VideoCompressor
{
    [TestClass]
    public class ParametersManagerTests
    {
        [TestMethod]
        public void CalculateVideoBitrate_ValidInput_ReturnsExpectedBitrate()
        {
            // Arrange
            int height = 720;
            int width = 1280;
            double bitrateCoefficient = 0.4;
            double frameRate = 30.0;

            // Act
            long result = ParametersManager.CalculateVideoBitrate(height, width, bitrateCoefficient, frameRate);

            // Assert
            long expectedBitrate = (long)(height * width * bitrateCoefficient * frameRate);
            Assert.AreEqual(expectedBitrate, result);
        }

        [TestMethod]
        public void CalculateVideoSize_ValidInput_ReturnsExpectedSize()
        {
            // Arrange
            int height = 720;
            int width = 1280;
            int targetResolution = 640;

            // Act
            (int calculatedHeight, int calculatedWidth) = ParametersManager.CalculateVideoSize(height, width, targetResolution);

            // Assert
            int expectedHeight = 360;  // Expected height based on aspect ratio
            int expectedWidth = 640;   // Expected width based on aspect ratio
            Assert.AreEqual(expectedHeight, calculatedHeight);
            Assert.AreEqual(expectedWidth, calculatedWidth);
        }

        [TestMethod]
        public void CalculateVideoSize_InvalidResolution_ThrowsArgumentException()
        {
            // Arrange
            int height = 720;
            int width = 1280;
            int targetResolution = 0; // Invalid resolution

            // Act and Assert
            Assert.ThrowsException<ArgumentException>(() => ParametersManager.CalculateVideoSize(height, width, targetResolution));
        }
    }
}
