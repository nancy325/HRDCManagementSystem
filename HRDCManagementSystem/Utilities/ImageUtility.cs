using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;

namespace HRDCManagementSystem.Utilities
{
    /// <summary>
    /// Provides cross-platform image generation utilities
    /// </summary>
    public static class ImageUtility
    {
        /// <summary>
        /// Determines if the current platform is Windows
        /// </summary>
        public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        /// <summary>
        /// Creates a default certificate template image file
        /// </summary>
        /// <param name="path">The full path to save the image</param>
        /// <param name="logger">Logger for capturing errors</param>
        /// <returns>True if successful, false otherwise</returns>
        public static async Task<bool> CreateDefaultCertificateTemplateAsync(string path, ILogger logger)
        {
            try
            {
                logger.LogInformation("CreateDefaultCertificateTemplateAsync: Creating template at {Path}", path);
                logger.LogInformation("Runtime OS: {OS}, IsWindows: {IsWindows}", 
                    RuntimeInformation.OSDescription,
                    IsWindows);

                // Create directory if it doesn't exist
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    logger.LogInformation("Creating directory: {Directory}", directory);
                    Directory.CreateDirectory(directory);
                }

                // Delete any existing file first to avoid issues
                if (File.Exists(path))
                {
                    logger.LogInformation("Removing existing template file at {Path}", path);
                    try
                    {
                        File.Delete(path);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Could not delete existing template file, will try to overwrite it");
                    }
                }

                // For Windows, we can use System.Drawing.Common
                if (IsWindows)
                {
                    logger.LogInformation("Using Windows-specific image generation");
                    try 
                    {
                        await CreateWindowsCertificateTemplateAsync(path);
                        logger.LogInformation("Windows certificate template created successfully");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error in Windows certificate template creation, falling back to basic template");
                        await CreateBasicTemplateAsync(path);
                    }
                }
                else
                {
                    logger.LogInformation("Using cross-platform image generation");
                    await CreateBasicTemplateAsync(path);
                }

                // Verify the file was created
                bool fileExists = File.Exists(path);
                logger.LogInformation("Template file created: {Result}", fileExists);
                
                if (fileExists)
                {
                    try
                    {
                        var fileInfo = new FileInfo(path);
                        logger.LogInformation("Template file size: {Size} bytes", fileInfo.Length);
                        
                        // If the file size is 0, delete it and try the basic method
                        if (fileInfo.Length == 0)
                        {
                            logger.LogWarning("Created template file is empty, trying again with basic template");
                            File.Delete(path);
                            await CreateBasicTemplateAsync(path);
                            
                            // Check again
                            fileInfo = new FileInfo(path);
                            logger.LogInformation("Template file created with basic method, size: {Size} bytes", fileInfo.Length);
                            return fileInfo.Length > 0;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error checking template file size");
                    }
                }
                else
                {
                    // Last resort - try once more with basic approach
                    logger.LogWarning("Template file not created, trying once more with basic template");
                    try
                    {
                        await CreateBasicTemplateAsync(path);
                        fileExists = File.Exists(path);
                        logger.LogInformation("Last attempt template creation: {Result}", fileExists);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Final attempt to create template failed");
                        return false;
                    }
                }

                return fileExists;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating certificate template at {Path}", path);
                return false;
            }
        }

        /// <summary>
        /// Creates a default signature image file
        /// </summary>
        /// <param name="path">The full path to save the image</param>
        /// <param name="logger">Logger for capturing errors</param>
        /// <returns>True if successful, false otherwise</returns>
        public static async Task<bool> CreateDefaultSignatureAsync(string path, ILogger logger)
        {
            try
            {
                logger.LogInformation("CreateDefaultSignatureAsync: Creating signature at {Path}", path);
                
                // Create directory if it doesn't exist
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    logger.LogInformation("Creating directory: {Directory}", directory);
                    Directory.CreateDirectory(directory);
                }

                // Delete any existing file first to avoid issues
                if (File.Exists(path))
                {
                    logger.LogInformation("Removing existing signature file at {Path}", path);
                    try
                    {
                        File.Delete(path);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Could not delete existing signature file, will try to overwrite it");
                    }
                }

                // For Windows, we can use System.Drawing.Common
                if (IsWindows)
                {
                    logger.LogInformation("Using Windows-specific signature generation");
                    try 
                    {
                        await CreateWindowsSignatureAsync(path);
                        logger.LogInformation("Windows signature created successfully");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error in Windows signature creation, falling back to basic signature");
                        await CreateBasicSignatureAsync(path);
                    }
                }
                else
                {
                    logger.LogInformation("Using cross-platform signature generation");
                    await CreateBasicSignatureAsync(path);
                }

                // Verify the file was created
                bool fileExists = File.Exists(path);
                logger.LogInformation("Signature file created: {Result}", fileExists);
                
                if (fileExists)
                {
                    try
                    {
                        var fileInfo = new FileInfo(path);
                        logger.LogInformation("Signature file size: {Size} bytes", fileInfo.Length);
                        
                        // If the file size is 0, delete it and try the basic method
                        if (fileInfo.Length == 0)
                        {
                            logger.LogWarning("Created signature file is empty, trying again with basic signature");
                            File.Delete(path);
                            await CreateBasicSignatureAsync(path);
                            
                            // Check again
                            fileInfo = new FileInfo(path);
                            logger.LogInformation("Signature file created with basic method, size: {Size} bytes", fileInfo.Length);
                            return fileInfo.Length > 0;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error checking signature file size");
                    }
                }
                else
                {
                    // Last resort - try once more with basic approach
                    logger.LogWarning("Signature file not created, trying once more with basic signature");
                    try
                    {
                        await CreateBasicSignatureAsync(path);
                        fileExists = File.Exists(path);
                        logger.LogInformation("Last attempt signature creation: {Result}", fileExists);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Final attempt to create signature failed");
                        return false;
                    }
                }

                return fileExists;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating signature image at {Path}", path);
                return false;
            }
        }

        /// <summary>
        /// Creates a basic template for non-Windows platforms or as fallback
        /// Uses a simple hardcoded approach to ensure something is created
        /// </summary>
        private static async Task CreateBasicTemplateAsync(string path)
        {
            using FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write);
            
            // Use a built-in minimal JPEG
            byte[] minimalJpeg = GetMinimalJpegBytes();
            
            await fs.WriteAsync(minimalJpeg, 0, minimalJpeg.Length);
            await fs.FlushAsync();
        }

        /// <summary>
        /// Creates a basic signature for non-Windows platforms or as fallback
        /// </summary>
        private static async Task CreateBasicSignatureAsync(string path)
        {
            using FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write);
            
            // Use a built-in minimal PNG
            byte[] minimalPng = GetMinimalPngBytes();
            
            await fs.WriteAsync(minimalPng, 0, minimalPng.Length);
            await fs.FlushAsync();
        }

        // This method is annotated to indicate it's only supported on Windows
        [SupportedOSPlatform("windows")]
        private static async Task CreateWindowsCertificateTemplateAsync(string path)
        {
            try
            {
                using (var bitmap = new System.Drawing.Bitmap(1754, 1240)) // A4 landscape at 150 DPI
                {
                    using (var g = System.Drawing.Graphics.FromImage(bitmap))
                    {
                        g.FillRectangle(
                            new System.Drawing.SolidBrush(System.Drawing.Color.White), 
                            0, 0, bitmap.Width, bitmap.Height);
                        
                        // Add a border
                        using (var pen = new System.Drawing.Pen(
                            System.Drawing.Color.FromArgb(180, 180, 180), 5))
                        {
                            g.DrawRectangle(pen, 20, 20, bitmap.Width - 40, bitmap.Height - 40);
                        }

                        // Add decorative elements
                        using (var cornerPen = new System.Drawing.Pen(
                            System.Drawing.Color.FromArgb(100, 100, 220), 3))
                        {
                            // Top-left corner
                            g.DrawLine(cornerPen, 20, 60, 100, 60);
                            g.DrawLine(cornerPen, 60, 20, 60, 100);
                            
                            // Top-right corner
                            g.DrawLine(cornerPen, bitmap.Width - 100, 60, bitmap.Width - 20, 60);
                            g.DrawLine(cornerPen, bitmap.Width - 60, 20, bitmap.Width - 60, 100);
                            
                            // Bottom-left corner
                            g.DrawLine(cornerPen, 20, bitmap.Height - 60, 100, bitmap.Height - 60);
                            g.DrawLine(cornerPen, 60, bitmap.Height - 100, 60, bitmap.Height - 20);
                            
                            // Bottom-right corner
                            g.DrawLine(cornerPen, bitmap.Width - 100, bitmap.Height - 60, bitmap.Width - 20, bitmap.Height - 60);
                            g.DrawLine(cornerPen, bitmap.Width - 60, bitmap.Height - 100, bitmap.Width - 60, bitmap.Height - 20);
                        }
                    }

                    bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Jpeg);
                }
            }
            catch
            {
                // If System.Drawing fails, try direct file creation as fallback
                await CreateBasicTemplateAsync(path);
            }

            await Task.CompletedTask; // To make the method async
        }

        // This method is annotated to indicate it's only supported on Windows
        [SupportedOSPlatform("windows")]
        private static async Task CreateWindowsSignatureAsync(string path)
        {
            try
            {
                using (var bitmap = new System.Drawing.Bitmap(300, 100))
                {
                    using (var g = System.Drawing.Graphics.FromImage(bitmap))
                    {
                        g.FillRectangle(
                            new System.Drawing.SolidBrush(System.Drawing.Color.White),
                            0, 0, bitmap.Width, bitmap.Height);
                        
                        // Draw signature lines
                        using (var pen = new System.Drawing.Pen(System.Drawing.Color.Black, 2))
                        {
                            g.DrawLine(pen, 20, 60, 80, 40);
                            g.DrawLine(pen, 80, 40, 120, 70);
                            g.DrawLine(pen, 120, 70, 180, 30);
                            g.DrawLine(pen, 180, 30, 260, 80);
                        }
                    }

                    bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Png);
                }
            }
            catch
            {
                // If System.Drawing fails, try direct file creation as fallback
                await CreateBasicSignatureAsync(path);
            }

            await Task.CompletedTask; // To make the method async
        }
        
        /// <summary>
        /// Get bytes for a minimal valid JPEG image
        /// </summary>
        private static byte[] GetMinimalJpegBytes()
        {
            // This is a valid 10x10 white JPEG image
            return new byte[] {
                0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01, 0x01, 0x01, 0x00, 0x48,
                0x00, 0x48, 0x00, 0x00, 0xFF, 0xDB, 0x00, 0x43, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                0xFF, 0xFF, 0xC0, 0x00, 0x11, 0x08, 0x00, 0x0A, 0x00, 0x0A, 0x03, 0x01, 0x22, 0x00, 0x02, 0x11,
                0x01, 0x03, 0x11, 0x01, 0xFF, 0xC4, 0x00, 0x14, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x09, 0xFF, 0xC4, 0x00, 0x14, 0x10, 0x01,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0xFF, 0xC4, 0x00, 0x15, 0x01, 0x01, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0xFF, 0xC4, 0x00, 0x14, 0x11, 0x01, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xDA, 0x00,
                0x0C, 0x03, 0x01, 0x00, 0x02, 0x11, 0x03, 0x11, 0x00, 0x3F, 0x00, 0x9D, 0x95, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0xFF, 0xD9
            };
        }
        
        /// <summary>
        /// Get bytes for a minimal valid PNG image
        /// </summary>
        private static byte[] GetMinimalPngBytes()
        {
            // This is a valid 10x10 white PNG image
            return new byte[] {
                0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
                0x00, 0x00, 0x00, 0x0A, 0x00, 0x00, 0x00, 0x0A, 0x08, 0x02, 0x00, 0x00, 0x00, 0x02, 0x50, 0x58,
                0xEA, 0x00, 0x00, 0x00, 0x01, 0x73, 0x52, 0x47, 0x42, 0x00, 0xAE, 0xCE, 0x1C, 0xE9, 0x00, 0x00,
                0x00, 0x04, 0x67, 0x41, 0x4D, 0x41, 0x00, 0x00, 0xB1, 0x8F, 0x0B, 0xFC, 0x61, 0x05, 0x00, 0x00,
                0x00, 0x09, 0x70, 0x48, 0x59, 0x73, 0x00, 0x00, 0x0E, 0xC3, 0x00, 0x00, 0x0E, 0xC3, 0x01, 0xC7,
                0x6F, 0xA8, 0x64, 0x00, 0x00, 0x00, 0x0C, 0x49, 0x44, 0x41, 0x54, 0x18, 0x57, 0x63, 0xF8, 0xCF,
                0xF0, 0x9F, 0x01, 0x00, 0x03, 0x03, 0x03, 0x00, 0x36, 0xD6, 0x29, 0x8D, 0x00, 0x00, 0x00, 0x00,
                0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82
            };
        }
    }
}