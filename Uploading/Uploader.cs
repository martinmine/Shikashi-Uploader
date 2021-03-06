﻿using Shikashi.API;
using Shikashi.Screenshotting;
using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Shikashi.Uploading
{
    class Uploader : IHotkeyListener
    {
        internal delegate void UploadResultReceived(FileUploadResult res);
        internal event UploadResultReceived OnUploadCompleted;
        private bool snipperOpen;
        private FileUpload uploadHelper;

        public Uploader(IUploadStatusListener progressStatusListener)
        {
            this.uploadHelper = new FileUpload(progressStatusListener);
        }
        
        internal async void CaptureScreen(string type)
        {
            switch (type)
            {
                case "screen":
                    using (TempScreenshotFile screenshot = ScreenCapture.CaptureScreen(Properties.Settings.Default.ShowCursor))
                    {
                        await HandleImageUpload(screenshot);
                    }

                    break;

                case "window":
                    using (TempScreenshotFile screenshot = ScreenCapture.Window(Properties.Settings.Default.ShowCursor))
                    {
                        await HandleImageUpload(screenshot);
                    }

                    break;

                default:
                    if (snipperOpen)
                        return;

                    snipperOpen = true;

                    try
                    {
                        Rectangle rect = Snipper.GetRegion();
                        if (rect == Rectangle.Empty)
                            return;
                        
                        using (TempScreenshotFile screenshot = ScreenCapture.CaptureRegion(rect))
                        {
                            await HandleImageUpload(screenshot);
                        }
                    }
                    finally
                    {
                        snipperOpen = false;
                    }

                    break;
            }
        }

        private async Task HandleImageUpload(TempScreenshotFile screenshot)
        {
            GlobalFunctions.PlaySound(Properties.Resources.capture);

            string nameSuffix = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
            FileUploadResult result = await uploadHelper.UploadFile(screenshot.FileStream, string.Format("Upload {0}.png", nameSuffix), "image/png", GetFileSize(screenshot.Path), screenshot.Path);
            OnUploadCompleted(result);
        }

        private long GetFileSize(string path)
        {
            return new FileInfo(path).Length;
        }

        internal async Task UploadFile(string path)
        {
            GlobalFunctions.PlaySound(Properties.Resources.capture);
            string extension = System.IO.Path.GetExtension(path);
            using (Stream fileStream = File.OpenRead(path))
            {
                string mimeType = MimeMapping.Instance.GetMimeType(extension);

                FileUploadResult result = await uploadHelper.UploadFile(fileStream, System.IO.Path.GetFileName(path), mimeType, GetFileSize(path), path);
                OnUploadCompleted(result);
            }
        }
        
        internal async Task UploadImage(Image image)
        {
            using (TempScreenshotFile file = new TempScreenshotFile(image))
                await HandleImageUpload(file);
        }

        void IHotkeyListener.OnKeyPress(Key key)
        {
            if (key == (Key)Properties.Settings.Default.WindowedScreenshotHotkeyValue)
            {
                CaptureScreen("window");
            }
            else if (key == (Key)Properties.Settings.Default.RegionalScreenshotHotkeyValue)
            {
                CaptureScreen("region");
            }
            else if (key == (Key)Properties.Settings.Default.FullScreenshotHotkeyValue)
            {
                CaptureScreen("screen");
            }
        }
    }
}
