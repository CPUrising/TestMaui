using Microsoft.Maui.Storage;
using CommunityToolkit.Maui.Storage;

namespace SeamlessLoopTest.Services
{
    public class FilePickerService
    {
        /// <summary>
        /// 选择音频文件
        /// </summary>
        public async Task<string?> PickAudioFileAsync()
        {
            try
            {
                var customFileType = new FilePickerFileType(
                    new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.iOS, new[] { "public.audio" } },
                        { DevicePlatform.Android, new[] { "audio/*" } },
                        { DevicePlatform.WinUI, new[] { ".wav", ".mp3", ".ogg", ".flac" } },
                        { DevicePlatform.macOS, new[] { "wav", "mp3", "ogg", "flac" } }
                    });

                var result = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "选择音频文件",
                    FileTypes = customFileType
                });

                return result?.FullPath;
            }
            catch (Exception)
            {
                // 用户取消或发生错误
                return null;
            }
        }

        /// <summary>
        /// 选择文件夹 (仅 Windows/macOS/Android 30+)
        /// </summary>
        public async Task<string?> PickFolderAsync()
        {
            try
            {
#if WINDOWS || MACCATALYST
                var result = await CommunityToolkit.Maui.Storage.FolderPicker.Default.PickAsync();
                return result?.Folder?.Path;
#else
                await Task.CompletedTask;
                return null;
#endif
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
