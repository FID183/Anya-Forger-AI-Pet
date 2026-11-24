using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using NAudio.CoreAudioApi;
using WpfAnimatedGif;

namespace DesktopCompanion
{
    public enum AnyaState { Idle, Falling, Splat, Talking, Whistling, Dancing }

    public partial class MainWindow : Window
    {
        private IntPtr _hwnd;
        private AnyaState _currentState = AnyaState.Idle;
        private readonly Dictionary<AnyaState, BitmapImage> _animations = new();

        private readonly AppSettings _settings;
        private readonly Brain _brain;
        private readonly VoiceEngine _voice;
        private MainframeWindow? _mainframeWindow;
        private readonly Random _rng = new();

        // Независимый высокочастотный игровой цикл (работает на 144-260 FPS даже при закрытом GUI)
        private CancellationTokenSource? _renderLoopCts;
        private double _exactLeft;
        private double _exactTop;

        private static readonly string[] SplatLines =
        {
            "Oof! Anya's face hurts...",
            "Gaaah! Mission failed!",
            "Why did you throw Anya?!",
            "Ouch... Need peanuts for recovery..."
        };

        private static readonly HashSet<string> MusicApps = new(StringComparer.OrdinalIgnoreCase)
        {
            "Spotify", "AIMP", "foobar2000", "MusicBee", "iTunes",
            "AppleMusic", "YandexMusic", "ZVUK", "VLC", "Winamp", "TIDAL", "Deezer"
        };

        // Физика
        private double _vx = 0, _vy = 0;
        private const double BounceFactor = 0.55;
        private const double GroundFriction = 0.82;
        private const double MaxThrowSpeed = 35.0;

        // Состояния
        private bool _isDragging = false;
        private bool _isGrounded = false;
        private bool _isHologram = false;
        private bool _floatingPinned = false;
        private bool _isTalking = false;
        private DateTime _splatUntil = DateTime.MinValue;
        private DateTime _danceUntil = DateTime.MinValue;

        // Автономная ходьба и таймер 3 секунд до Idle
        private bool _isAutonomousWalking = false;
        private double _walkDirection = 1.0;
        private DateTime _nextWanderSwitchTime = DateTime.Now.AddSeconds(4);
        private DateTime _stoppedWalkingTime = DateTime.Now.AddSeconds(-10);

        // Кэш музыки
        private DateTime _lastAudioCheck = DateTime.MinValue;
        private bool _cachedMusicPlaying = false;
        private float _cachedMusicVolume = 0f;
        private AnyaState _lockedMusicState = AnyaState.Whistling;
        private DateTime _musicStateLockUntil = DateTime.MinValue;

        // Перетаскивание мышью
        private Point _dragOffset;
        private Point _lastMouseScreenPos;
        private readonly Stopwatch _dragTimer = new();
        private DateTime _lastClickTime = DateTime.MinValue;

        // Трекинг окон и аудио
        private List<Win32Helper.RECT> _windowPlatforms = new();
        private int _tickCounter = 0;
        private double _dpiScaleX = 1.0, _dpiScaleY = 1.0;
        private MMDevice? _audioDevice;

        public MainWindow()
        {
            InitializeComponent();
            _settings = AppSettings.Current;
            _brain = new Brain(_settings);
            _voice = new VoiceEngine(_settings);

            PreloadAnimations();
            InitAudioDetector();
            ApplyLiveSettings();
        }

        private void PreloadAnimations()
        {
            _animations[AnyaState.Idle]      = LoadGif("idle.gif");
            _animations[AnyaState.Falling]   = LoadGif("falling.gif");
            _animations[AnyaState.Splat]     = LoadGif("splat.gif");
            _animations[AnyaState.Talking]   = LoadGif("talk.gif");
            _animations[AnyaState.Whistling] = LoadGif("whistle.gif");
            _animations[AnyaState.Dancing]   = LoadGif("dance.gif");
        }

        private static BitmapImage LoadGif(string fileName)
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri($"pack://application:,,,/Assets/{fileName}", UriKind.Absolute);
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }

        private void SetAnyaState(AnyaState newState)
        {
            if (_currentState == newState) return;
            _currentState = newState;
            if (_animations.TryGetValue(newState, out var gif))
                ImageBehavior.SetAnimatedSource(AnyaSprite, gif);
        }

        private void InitAudioDetector()
        {
            try
            {
                var enumerator = new MMDeviceEnumerator();
                _audioDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            }
            catch { }
        }

        private bool IsMusicAppPlaying(out float peakVolume)
        {
            if ((DateTime.Now - _lastAudioCheck).TotalMilliseconds < 500)
            {
                peakVolume = _cachedMusicVolume;
                return _cachedMusicPlaying;
            }

            _lastAudioCheck = DateTime.Now;
            _cachedMusicPlaying = false;
            _cachedMusicVolume = 0f;

            try
            {
                if (_audioDevice != null)
                {
                    var sessions = _audioDevice.AudioSessionManager.Sessions;
                    for (int i = 0; i < sessions.Count; i++)
                    {
                        var session = sessions[i];
                        uint pid = session.GetProcessID;
                        if (pid == 0) continue;

                        float vol = session.AudioMeterInformation.MasterPeakValue;
                        if (vol > (float)AppSettings.Current.MusicSensitivity)
                        {
                            using var proc = Process.GetProcessById((int)pid);
                            if (MusicApps.Contains(proc.ProcessName))
                            {
                                _cachedMusicPlaying = true;
                                _cachedMusicVolume = vol;
                                break;
                            }
                        }
                    }
                }
            }
            catch { }

            peakVolume = _cachedMusicVolume;
            return _cachedMusicPlaying;
        }

        public void ApplyLiveSettings()
        {
            this.Topmost = AppSettings.Current.AlwaysOnTop;
            double scaledSize = 150 * AppSettings.Current.SpriteScale;
            AnyaSprite.Width = scaledSize;
            AnyaSprite.Height = scaledSize;

            this.Width = Math.Max(240, scaledSize + 60);
            this.Height = scaledSize + 130;

            if (_floatingPinned)
            {
                _floatingPinned = false;
                _isGrounded = false;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _hwnd = new WindowInteropHelper(this).Handle;
            int exStyle = Win32Helper.GetWindowLong(_hwnd, Win32Helper.GWL_EXSTYLE);
            Win32Helper.SetWindowLong(_hwnd, Win32Helper.GWL_EXSTYLE, exStyle | Win32Helper.WS_EX_TOOLWINDOW);

            PresentationSource source = PresentationSource.FromVisual(this);
            if (source?.CompositionTarget != null)
            {
                _dpiScaleX = source.CompositionTarget.TransformToDevice.M11;
                _dpiScaleY = source.CompositionTarget.TransformToDevice.M22;
            }

            _exactLeft = SystemParameters.WorkArea.Width / 2 - this.Width / 2;
            _exactTop = 100;
            MoveWindowNative(_exactLeft, _exactTop);

            ImageBehavior.SetAnimatedSource(AnyaSprite, _animations[AnyaState.Idle]);

            // Включаем аппаратную точность таймера Windows 1 мс и запускаем независимый High-FPS цикл
            Win32Helper.timeBeginPeriod(1);
            _renderLoopCts = new CancellationTokenSource();
            _ = RunHighPrecisionLoopAsync(_renderLoopCts.Token);

            _ = SpeakAndBubbleAsync("Agent Anya reporting for duty! Waku waku!", 4500);
        }

        protected override void OnClosed(EventArgs e)
        {
            _renderLoopCts?.Cancel();
            Win32Helper.timeEndPeriod(1);
            base.OnClosed(e);
        }

        /// <summary>
        /// Собственный высокоточный цикл (держит 60..260 FPS независимо от того, открыт GUI или закрыт)
        /// </summary>
        private async Task RunHighPrecisionLoopAsync(CancellationToken token)
        {
            var frameClock = Stopwatch.StartNew();
            var stepClock = Stopwatch.StartNew();

            await Task.Run(() =>
            {
                while (!token.IsCancellationRequested)
                {
                    int targetFps = Math.Clamp(AppSettings.Current.TargetFps, 60, 260);
                    double targetFrameTime = 1.0 / targetFps;
                    double elapsed = frameClock.Elapsed.TotalSeconds;

                    if (elapsed < targetFrameTime)
                    {
                        double remainingMs = (targetFrameTime - elapsed) * 1000.0;
                        if (remainingMs > 1.5)
                            Thread.Sleep(1);
                        else
                            Thread.SpinWait(50);
                        continue;
                    }

                    frameClock.Restart();

                    double dtSeconds = Math.Clamp(stepClock.Elapsed.TotalSeconds, 0.002, 0.05);
                    stepClock.Restart();

                    try
                    {
                        Dispatcher.Invoke(() => OnPhysicsStep(dtSeconds), DispatcherPriority.Send, token);
                    }
                    catch (OperationCanceledException) { break; }
                    catch { }
                }
            }, token);
        }

        private void MoveWindowNative(double dipLeft, double dipTop)
        {
            int pixelX = (int)Math.Round(dipLeft * _dpiScaleX);
            int pixelY = (int)Math.Round(dipTop * _dpiScaleY);
            Win32Helper.MoveWindowFast(_hwnd, pixelX, pixelY);
        }

        private void OnPhysicsStep(double dtSeconds)
        {
            double timeScale = dtSeconds * 60.0;
            _tickCounter++;

            if (_isHologram && (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt)))
                ToggleHologramMode(false);

            // 1. Перетаскивание мышью
            if (_isDragging)
            {
                SetAnyaState(AnyaState.Falling);
                Point currentMouse = GetMouseScreenPositionDpi();
                double dragDt = Math.Max(_dragTimer.Elapsed.TotalSeconds, 0.003);

                _vx = Math.Clamp((currentMouse.X - _lastMouseScreenPos.X) / (dragDt * 60.0), -MaxThrowSpeed, MaxThrowSpeed);
                _vy = Math.Clamp((currentMouse.Y - _lastMouseScreenPos.Y) / (dragDt * 60.0), -MaxThrowSpeed, MaxThrowSpeed);

                _exactLeft = currentMouse.X - _dragOffset.X;
                _exactTop = currentMouse.Y - _dragOffset.Y;
                MoveWindowNative(_exactLeft, _exactTop);

                _lastMouseScreenPos = currentMouse;
                _dragTimer.Restart();
                return;
            }

            if (_floatingPinned) return;

            if (_tickCounter % 20 == 0)
                _windowPlatforms = Win32Helper.GetWindowPlatforms(_hwnd);

            // 2. Расчет гравитации
            double impactVelocityY = _vy;
            _vy += AppSettings.Current.Gravity * timeScale;
            _vx *= _isGrounded ? Math.Pow(GroundFriction, timeScale) : Math.Pow(0.99, timeScale);

            double nextLeft = _exactLeft + (_vx * timeScale);
            double nextTop = _exactTop + (_vy * timeScale);
            Rect workArea = SystemParameters.WorkArea;
            bool landedHard = false;
            _isGrounded = false;

            // Посадка на верхние грани открытых окон
            if (_vy >= 0)
            {
                double bottomNow = _exactTop + this.Height;
                double bottomNext = nextTop + this.Height;
                double centerX = nextLeft + (this.Width / 2.0);

                foreach (var rect in _windowPlatforms)
                {
                    double winTop = rect.Top / _dpiScaleY;
                    double winLeft = rect.Left / _dpiScaleX;
                    double winRight = rect.Right / _dpiScaleX;

                    if (centerX >= winLeft + 15 && centerX <= winRight - 15)
                    {
                        if (bottomNow <= winTop + 14 && bottomNext >= winTop)
                        {
                            nextTop = winTop - this.Height;
                            landedHard = impactVelocityY > 12.0;
                            _vy = 0;
                            _isGrounded = true;
                            break;
                        }
                    }
                }
            }

            // Посадка на панель задач (пол)
            if (nextTop + this.Height >= workArea.Bottom)
            {
                nextTop = workArea.Bottom - this.Height;
                if (impactVelocityY > 12.0)
                    landedHard = true;
                _vy = 0;
                _isGrounded = true;
            }

            if (landedHard)
            {
                _splatUntil = DateTime.Now.AddMilliseconds(1800);
                _vx = 0;
                _isAutonomousWalking = false;
                _stoppedWalkingTime = DateTime.Now;
                _ = SpeakAndBubbleAsync(SplatLines[_rng.Next(SplatLines.Length)], 2600);
            }

            // 3. Обновляем анимацию и плавную ходьбу
            UpdateBehaviorAndAnimation(ref nextLeft, dtSeconds);

            // Ограничение краев экрана
            if (nextLeft < workArea.Left)
            {
                nextLeft = workArea.Left;
                _vx = -_vx * BounceFactor;
                _walkDirection = 1.0;
            }
            else if (nextLeft + this.Width > workArea.Right)
            {
                nextLeft = workArea.Right - this.Width;
                _vx = -_vx * BounceFactor;
                _walkDirection = -1.0;
            }
            if (nextTop < workArea.Top)
            {
                nextTop = workArea.Top;
                _vy = -_vy * BounceFactor;
            }

            _exactLeft = nextLeft;
            _exactTop = nextTop;
            MoveWindowNative(_exactLeft, _exactTop);
        }

        private void UpdateBehaviorAndAnimation(ref double nextLeft, double dtSeconds)
        {
            if (!_isGrounded)
            {
                SetAnyaState(AnyaState.Falling);
                return;
            }

            if (DateTime.Now < _splatUntil)
            {
                SetAnyaState(AnyaState.Splat);
                return;
            }

            if (DateTime.Now < _danceUntil)
            {
                SetAnyaState(AnyaState.Dancing);
                return;
            }

            if (_isTalking)
            {
                SetAnyaState(AnyaState.Talking);
                return;
            }

            // Музыка из Spotify и музыкальных плееров
            if (IsMusicAppPlaying(out float volume))
            {
                if (DateTime.Now > _musicStateLockUntil)
                {
                    _lockedMusicState = volume > 0.28f ? AnyaState.Dancing : AnyaState.Whistling;
                    _musicStateLockUntil = DateTime.Now.AddSeconds(2.5);
                }

                SetAnyaState(_lockedMusicState);

                if (_lockedMusicState == AnyaState.Whistling && AppSettings.Current.AutonomousWalking)
                {
                    nextLeft += _walkDirection * 42.0 * dtSeconds;
                    SpriteFlip.ScaleX = _walkDirection < 0 ? 1 : -1;
                }
                _stoppedWalkingTime = DateTime.Now;
                return;
            }

            if (!AppSettings.Current.AutonomousWalking)
            {
                SetAnyaState(AnyaState.Idle);
                return;
            }

            DateTime now = DateTime.Now;
            if (now >= _nextWanderSwitchTime)
            {
                if (_isAutonomousWalking)
                {
                    _isAutonomousWalking = false;
                    _stoppedWalkingTime = now;
                    _nextWanderSwitchTime = now.AddSeconds(_rng.Next(5, 10));
                }
                else
                {
                    _isAutonomousWalking = true;
                    _walkDirection = _rng.Next(0, 2) == 0 ? -1.0 : 1.0;
                    _nextWanderSwitchTime = now.AddSeconds(_rng.Next(3, 8));
                }
            }

            if (_isAutonomousWalking)
            {
                nextLeft += _walkDirection * 45.0 * dtSeconds;
                SpriteFlip.ScaleX = _walkDirection < 0 ? 1 : -1;
                SetAnyaState(AnyaState.Talking);
            }
            else
            {
                if ((now - _stoppedWalkingTime).TotalSeconds >= 3.0)
                {
                    SpriteFlip.ScaleX = 1;
                    SetAnyaState(AnyaState.Idle);
                }
            }
        }

        private void Sprite_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if ((DateTime.Now - _lastClickTime).TotalMilliseconds < 300)
            {
                ToggleChatInput();
                return;
            }
            _lastClickTime = DateTime.Now;

            _isDragging = true;
            _floatingPinned = false;
            _isAutonomousWalking = false;
            _splatUntil = DateTime.MinValue;

            AnyaSprite.CaptureMouse();
            _dragOffset = e.GetPosition(this);
            _lastMouseScreenPos = GetMouseScreenPositionDpi();
            _dragTimer.Restart();
        }

        private void Sprite_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isDragging) return;
            _isDragging = false;
            AnyaSprite.ReleaseMouseCapture();
            _stoppedWalkingTime = DateTime.Now;

            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                _floatingPinned = true;
                _vx = 0; _vy = 0;
                SetAnyaState(AnyaState.Idle);
                _ = ShowSpeechBubbleAsync("Floating mode activated! Heh.", 2200);
            }
        }

        private void ToggleChatInput()
        {
            ChatInputBox.Visibility = ChatInputBox.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;
            if (ChatInputBox.Visibility == Visibility.Visible)
                UserPromptInput.Focus();
        }

        private async void UserPromptInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(UserPromptInput.Text))
            {
                string userText = UserPromptInput.Text.Trim();
                UserPromptInput.Text = "";
                ChatInputBox.Visibility = Visibility.Collapsed;

                _isTalking = true;
                await ShowSpeechBubbleAsync("Reading your thoughts...", 1200);

                var activeWindows = Win32Helper.GetActiveWindowTitles(_hwnd);
                string reply = await _brain.AskAnyaAsync(userText, activeWindows);
                await SpeakAndBubbleAsync(reply, 6000);
                _isTalking = false;
                _stoppedWalkingTime = DateTime.Now;
            }
            else if (e.Key == Key.Escape)
            {
                ChatInputBox.Visibility = Visibility.Collapsed;
            }
        }

        private void MenuSettings_Click(object sender, RoutedEventArgs e)
        {
            if (_mainframeWindow == null || !_mainframeWindow.IsLoaded)
            {
                _mainframeWindow = new MainframeWindow(AppSettings.Current, _brain, _voice, ApplyLiveSettings);
                _mainframeWindow.Show();
            }
            else
            {
                _mainframeWindow.Activate();
            }
        }

        private void MenuTalk_Click(object sender, RoutedEventArgs e) => ToggleChatInput();

        private async void MenuTelepathy_Click(object sender, RoutedEventArgs e)
        {
            _isTalking = true;
            await ShowSpeechBubbleAsync("*Telepathy noises* Scanning your windows...", 1500);
            var activeWindows = Win32Helper.GetActiveWindowTitles(_hwnd);
            string reply = await _brain.AskAnyaAsync("Use your telepathy to comment on the apps and windows I currently have open!", activeWindows);
            await SpeakAndBubbleAsync(reply, 6500);
            _isTalking = false;
            _stoppedWalkingTime = DateTime.Now;
        }

        private void MenuPeanuts_Click(object sender, RoutedEventArgs e)
        {
            _danceUntil = DateTime.Now.AddSeconds(3.5);
            _ = SpeakAndBubbleAsync("PEANUTS!! Waku waku!", 3500);
        }

        private void MenuHologram_Click(object sender, RoutedEventArgs e) => ToggleHologramMode(true);

        private void MenuExit_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

        public async Task SpeakAndBubbleAsync(string text, int durationMs)
        {
            BubbleText.Text = text;
            SpeechBubble.Visibility = Visibility.Visible;
            var voiceTask = _voice.SpeakAsync(text);
            var delayTask = Task.Delay(durationMs);
            await Task.WhenAll(voiceTask, delayTask);
            if (BubbleText.Text == text)
                SpeechBubble.Visibility = Visibility.Collapsed;
        }

        public async Task ShowSpeechBubbleAsync(string text, int durationMs)
        {
            BubbleText.Text = text;
            SpeechBubble.Visibility = Visibility.Visible;
            await Task.Delay(durationMs);
            if (BubbleText.Text == text)
                SpeechBubble.Visibility = Visibility.Collapsed;
        }

        private void ToggleHologramMode(bool enable)
        {
            _isHologram = enable;
            Win32Helper.SetClickThrough(_hwnd, enable);
            AnyaSprite.Opacity = enable ? 0.45 : 1.0;
            if (enable)
                _ = ShowSpeechBubbleAsync("Hologram mode! Hold ALT to touch Anya again.", 3000);
        }

        private Point GetMouseScreenPositionDpi()
        {
            Win32Helper.GetCursorPos(out Win32Helper.POINT pt);
            return new Point(pt.X / _dpiScaleX, pt.Y / _dpiScaleY);
        }
    }
}