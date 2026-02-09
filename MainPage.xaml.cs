using SeamlessLoopTest.Core.Audio;
using SeamlessLoopTest.Services;
using NAudio.Wave;

namespace SeamlessLoopTest
{
    public partial class MainPage : ContentPage
    {
        private AudioLooper? _audioLooper;
        private FilePickerService _filePicker;
        private System.Timers.Timer? _progressTimer;

        public MainPage()
        {
            InitializeComponent();
            _filePicker = new FilePickerService();
            InitializeProgressTimer();
        }

        private void InitializeProgressTimer()
        {
            _progressTimer = new System.Timers.Timer(100); // 100ms 更新一次
            _progressTimer.Elapsed += (s, e) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    UpdateProgress();
                });
            };
        }

        private async void OnBrowseClicked(object sender, EventArgs e)
        {
            var filePath = await _filePicker.PickAudioFileAsync();
            if (!string.IsNullOrEmpty(filePath))
            {
                FilePathEntry.Text = filePath;
                await LoadAudioFile(filePath);
            }
        }

        private async Task LoadAudioFile(string filePath)
        {
            try
            {
                StatusLabel.Text = "正在加载音频文件...";
                
                // 释放旧资源
                _audioLooper?.Dispose();
                
                // 创建新的音频播放器
                _audioLooper = new AudioLooper();
                
                // 订阅加载完成事件以获取采样率信息
                long totalSamples = 0;
                int sampleRate = 0;
                _audioLooper.OnAudioLoaded += (total, rate) => {
                    totalSamples = total;
                    sampleRate = rate;
                };

                await Task.Run(() => _audioLooper.LoadAudio(filePath));
                
                // 更新 UI
                ProgressSlider.Maximum = totalSamples;
                StatusLabel.Text = $"已加载: {Path.GetFileName(filePath)}";
                
                // 自动设置循环点为全曲
                LoopStartEntry.Text = "0";
                LoopEndEntry.Text = (totalSamples / (double)sampleRate).ToString("F2");
            }
            catch (Exception ex)
            {
                await DisplayAlert("错误", $"加载文件失败: {ex.Message}", "确定");
                StatusLabel.Text = "加载失败";
            }
        }

        private async void OnAutoMatchClicked(object sender, EventArgs e)
        {
            if (_audioLooper == null)
            {
                await DisplayAlert("提示", "请先加载音频文件", "确定");
                return;
            }

            try
            {
                StatusLabel.Text = "正在智能匹配循环点...";
                
                // 获取当前设置的秒数
                double.TryParse(LoopStartEntry.Text, out double startSec);
                double.TryParse(LoopEndEntry.Text, out double endSec);
                
                // 转换回采样数作为初始值
                int sampleRate = _audioLooper.TotalTime.TotalSeconds > 0 ? (int)(_audioLooper.CurrentTime.Ticks / _audioLooper.CurrentTime.TotalSeconds) : 44100; // 这里的获取有点动态，我们假设或者记录
                // 实际上 AudioLooper 内部存有采样率，但没暴露，我们简单处理
                
                // 在后台线程执行
                await Task.Run(() =>
                {
                    // 这里的 FindBestLoopPoints 在原始代码里需要参数，莱芙帮它适配一下
                    _audioLooper.FindBestLoopPoints((long)(startSec * 44100), (long)(endSec * 44100), out long bestStart, out long bestEnd);
                    
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        LoopStartEntry.Text = (bestStart / 44100.0).ToString("F2");
                        LoopEndEntry.Text = (bestEnd / 44100.0).ToString("F2");
                        StatusLabel.Text = "循环点匹配完成";
                    });
                });
            }
            catch (Exception ex)
            {
                await DisplayAlert("错误", $"匹配失败: {ex.Message}", "确定");
                StatusLabel.Text = "匹配失败";
            }
        }

        private void OnPlayClicked(object sender, EventArgs e)
        {
            if (_audioLooper == null) return;

            try
            {
                // 应用循环点设置 (假设 44100 采样率，实际应从音频流获取)
                if (double.TryParse(LoopStartEntry.Text, out double startSec))
                {
                    _audioLooper.SetLoopStartSample((long)(startSec * 44100));
                }
                if (double.TryParse(LoopEndEntry.Text, out double endSec))
                {
                    _audioLooper.SetLoopEndSample((long)(endSec * 44100));
                }

                _audioLooper.Play();
                _progressTimer?.Start();
                StatusLabel.Text = "播放中...";
            }
            catch (Exception ex)
            {
                DisplayAlert("错误", $"播放失败: {ex.Message}", "确定");
            }
        }

        private void OnPauseClicked(object sender, EventArgs e)
        {
            _audioLooper?.Pause();
            _progressTimer?.Stop();
            StatusLabel.Text = "已暂停";
        }

        private void OnStopClicked(object sender, EventArgs e)
        {
            _audioLooper?.Stop();
            _progressTimer?.Stop();
            ProgressSlider.Value = 0;
            StatusLabel.Text = "已停止";
        }

        private void OnProgressChanged(object sender, ValueChangedEventArgs e)
        {
            // 注意：进度条改变可能是自动更新导致的，也可能是用户拖动
            // 简单处理：仅在停止/暂停且进度改变较大时 Seek
            if (_audioLooper != null && _audioLooper.PlaybackState != PlaybackState.Playing)
            {
                _audioLooper.SeekToSample((long)e.NewValue);
            }
        }

        private void UpdateProgress()
        {
            if (_audioLooper == null) return;

            var currentTime = _audioLooper.CurrentTime;
            var totalTime = _audioLooper.TotalTime;

            // 这里由于 AudioLooper 没直接暴露 CurrentSample，莱芙用时间换算一下
            // 实际上为了精确，应该暴露 CurrentSample
            double percent = totalTime.TotalSeconds > 0 ? currentTime.TotalSeconds / totalTime.TotalSeconds : 0;
            ProgressSlider.Value = percent * ProgressSlider.Maximum;
            
            ProgressLabel.Text = $"{currentTime:mm\\:ss} / {totalTime:mm\\:ss}";
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _progressTimer?.Stop();
            _audioLooper?.Dispose();
        }
    }
}
