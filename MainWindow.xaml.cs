using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml.Serialization;
using Microsoft.Win32;
using WinForms = System.Windows.Forms;
using WpfControls = System.Windows.Controls;

namespace crosshair3
{
    public partial class MainWindow : Window
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        // Constants for window positioning and styles
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint TOPMOST_FLAGS = SWP_NOMOVE | SWP_NOSIZE;
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TRANSPARENT = 0x20;

        private const int HOTKEY_ID = 9000;
        private IntPtr windowHandle;

        private Window? crosshairWindow;
        private bool isPinned = false;
        private double originalWidth = 32;
        private double originalHeight = 32;

        private class CrosshairInfo
        {
            public string Name { get; set; }
            public string Path { get; set; }
            public BitmapImage Image { get; set; }
            public double Left { get; set; }
            public double Top { get; set; }
            public bool IsCentered { get; set; }
            public double SizeValue { get; set; } = 100;
        }

        private List<CrosshairInfo> crosshairs = new List<CrosshairInfo>()
        {
            new CrosshairInfo { Name = "(Empty)", Path = "", Image = null },
            new CrosshairInfo { Name = "(Empty)", Path = "", Image = null },
            new CrosshairInfo { Name = "(Empty)", Path = "", Image = null }
        };

        private int currentCrosshairIndex = 0;
        private Key switchHotKey = Key.D0; // Default hotkey is 0

        private CrosshairSettings settings = new CrosshairSettings();
        private readonly string settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CrosshairOverlay",
            "settings.xml");

        private DispatcherTimer topmostTimer;

        public MainWindow()
        {
            InitializeComponent();

            // Set window title using version info
            this.Title = ProgramInfo.FULL_NAME;

            LoadSettings();
            CreateCrosshairWindow();

            // Get the window handle after initialization
            windowHandle = new WindowInteropHelper(this).Handle;

            // Register for window messages
            ComponentDispatcher.ThreadPreprocessMessage += ComponentDispatcher_ThreadPreprocessMessage;

            RegisterHotKey();

            // Load saved crosshairs
            for (int i = 0; i < settings.CrosshairPaths.Length; i++)
            {
                if (!string.IsNullOrEmpty(settings.CrosshairPaths[i]))
                {
                    try
                    {
                        var newImage = new BitmapImage(new Uri(settings.CrosshairPaths[i]));
                        string fileName = Path.GetFileName(settings.CrosshairPaths[i]);
                        crosshairs[i] = new CrosshairInfo
                        {
                            Name = fileName,
                            Path = settings.CrosshairPaths[i],
                            Image = newImage
                        };
                    }
                    catch { /* Ignore loading errors */ }
                }
            }

            UpdateCrosshairList();

            // Set last used crosshair
            currentCrosshairIndex = settings.LastUsedIndex;
            if (crosshairs[currentCrosshairIndex].Image != null)
            {
                UpdateDisplayedCrosshair(currentCrosshairIndex);
            }

            // Apply saved size
            SizeSlider.Value = settings.SizeValue;

            // Apply saved position
            if (crosshairWindow != null)
            {
                crosshairWindow.Left = settings.WindowLeftPositions[currentCrosshairIndex];
                crosshairWindow.Top = settings.WindowTopPositions[currentCrosshairIndex];
                crosshairWindow.Visibility = settings.IsActive ? Visibility.Visible : Visibility.Collapsed;
            }

            // Register for keyboard events
            this.KeyDown += MainWindow_KeyDown;

            // Create and start the topmost timer
            topmostTimer = new DispatcherTimer();
            topmostTimer.Interval = TimeSpan.FromSeconds(25);
            topmostTimer.Tick += (s, e) => EnsureTopMostOverlay();
            topmostTimer.Start();

            // Update default folder display
            UpdateDefaultFolderDisplay();
        }

        private void ComponentDispatcher_ThreadPreprocessMessage(ref MSG msg, ref bool handled)
        {
            if (msg.message == 0x0312 && msg.wParam.ToInt32() == HOTKEY_ID)
            {
                SwitchToNextCrosshair();
                handled = true;
            }
        }

        private void CreateCrosshairWindow()
        {
            crosshairWindow = new Window
            {
                Width = originalWidth,
                Height = originalHeight,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = System.Windows.Media.Brushes.Transparent,
                Topmost = true,
                ShowInTaskbar = false,
                ResizeMode = ResizeMode.NoResize
            };

            // Try to load a default crosshair from the default folder
            BitmapImage defaultImage = GetDefaultCrosshairImage();
            
            System.Windows.Controls.Image crosshairImage = new System.Windows.Controls.Image
            {
                Source = defaultImage,
                Width = originalWidth,
                Height = originalHeight,
                Stretch = Stretch.Uniform
            };
            crosshairWindow.Content = crosshairImage;

            // Show the window before modifying its properties
            crosshairWindow.Show();

            // Get the window handle
            var handle = new WindowInteropHelper(crosshairWindow).Handle;

            // Set window to layered and transparent
            int exStyle = GetWindowLong(handle, GWL_EXSTYLE);
            exStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT;
            SetWindowLong(handle, GWL_EXSTYLE, exStyle);

            // Ensure it's topmost
            SetWindowPos(handle, HWND_TOPMOST, 0, 0, 0, 0, TOPMOST_FLAGS);

            // Start the topmost timer
            StartTopmostTimer();
        }

        private void UpdateDragability()
        {
            // Empty - no longer needed
        }

        private void ToggleCrosshairButton_Click(object sender, RoutedEventArgs e)
        {
            if (crosshairWindow == null) return;

            crosshairWindow.Visibility = crosshairWindow.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private void SelectCrosshairButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "PNG files (*.png)|*.png|All files (*.*)|*.*";
            openFileDialog.InitialDirectory = settings.DefaultCrosshairFolder;
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var newImage = new BitmapImage(new Uri(openFileDialog.FileName));

                    originalWidth = newImage.PixelWidth;
                    originalHeight = newImage.PixelHeight;

                    SizeSlider.Value = 100;

                    ((System.Windows.Controls.Image)crosshairWindow!.Content).Source = newImage;

                    crosshairWindow.Width = originalWidth;
                    crosshairWindow.Height = originalHeight;
                    ((System.Windows.Controls.Image)crosshairWindow.Content).Width = originalWidth;
                    ((System.Windows.Controls.Image)crosshairWindow.Content).Height = originalHeight;
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error loading image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CenterCrosshairButton_Click(object sender, RoutedEventArgs e)
        {
            if (crosshairWindow == null) return;

            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;
            double centerX = (screenWidth - crosshairWindow.Width) / 2;
            double centerY = (screenHeight - crosshairWindow.Height) / 2;

            crosshairWindow.Left = centerX;
            crosshairWindow.Top = centerY;

            // Store the centered position for the current crosshair
            crosshairs[currentCrosshairIndex].Left = centerX;
            crosshairs[currentCrosshairIndex].Top = centerY;
            crosshairs[currentCrosshairIndex].IsCentered = true;

            // Update position display (will show 0,0 when centered)
            UpdatePositionDisplay();
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            // Bring the crosshair window to the front when this window is activated
            crosshairWindow.Topmost = true;
        }

        protected override void OnDeactivated(EventArgs e)
        {
            base.OnDeactivated(e);
            // Keep the crosshair window on top when this window is deactivated
            crosshairWindow.Topmost = true;
        }

        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            if (crosshairWindow != null)
            {
                crosshairWindow.Close();
            }
        }

        private void PinCrosshairButton_Click(object sender, RoutedEventArgs e)
        {
            if (crosshairWindow == null) return;

            isPinned = !isPinned;
            UpdateDragability();

            ((System.Windows.Controls.Button)sender).Content = isPinned ? "Unpin" : "Pin";
        }

        private void SizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (crosshairWindow == null) return;

            double scale = e.NewValue / 100.0;

            double newWidth = originalWidth * scale;
            double newHeight = originalHeight * scale;

            crosshairWindow.Width = newWidth;
            crosshairWindow.Height = newHeight;

            var image = (System.Windows.Controls.Image)crosshairWindow.Content;
            image.Width = newWidth;
            image.Height = newHeight;

            if (e.NewValue != e.OldValue)
            {
                double deltaWidth = (newWidth - (originalWidth * (e.OldValue / 100.0))) / 2;
                double deltaHeight = (newHeight - (originalHeight * (e.OldValue / 100.0))) / 2;

                crosshairWindow.Left -= deltaWidth;
                crosshairWindow.Top -= deltaHeight;
            }
        }

        private void UpdateCrosshairList()
        {
            CrosshairListBox.Items.Clear();
            for (int i = 0; i < crosshairs.Count; i++)
            {
                CrosshairListBox.Items.Add($"({i + 1}) {crosshairs[i].Name}");
            }
        }

        private void SelectCrosshair1Button_Click(object sender, RoutedEventArgs e)
        {
            SelectCrosshair(0);
        }

        private void SelectCrosshair2Button_Click(object sender, RoutedEventArgs e)
        {
            SelectCrosshair(1);
        }

        private void SelectCrosshair3Button_Click(object sender, RoutedEventArgs e)
        {
            SelectCrosshair(2);
        }

        private void SelectCrosshair(int index)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "PNG files (*.png)|*.png|All files (*.*)|*.*";
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var newImage = new BitmapImage(new Uri(openFileDialog.FileName));
                    string fileName = System.IO.Path.GetFileName(openFileDialog.FileName);

                    // Calculate center position
                    double screenWidth = SystemParameters.PrimaryScreenWidth;
                    double screenHeight = SystemParameters.PrimaryScreenHeight;
                    double centerX = (screenWidth - newImage.PixelWidth) / 2;
                    double centerY = (screenHeight - newImage.PixelHeight) / 2;

                    crosshairs[index] = new CrosshairInfo
                    {
                        Name = fileName,
                        Path = openFileDialog.FileName,
                        Image = newImage,
                        Left = centerX,
                        Top = centerY,
                        IsCentered = true
                    };

                    UpdateCrosshairList();

                    // If this is the current crosshair, update the display
                    if (currentCrosshairIndex == index)
                    {
                        UpdateDisplayedCrosshair(index);
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error loading image: {ex.Message}", "Error",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void UpdateDisplayedCrosshair(int index)
        {
            if (crosshairs[index].Image != null)
            {
                var newImage = crosshairs[index].Image;
                originalWidth = newImage.PixelWidth;
                originalHeight = newImage.PixelHeight;

                // Apply saved size value to slider
                SizeSlider.Value = crosshairs[index].SizeValue;

                                    ((System.Windows.Controls.Image)crosshairWindow!.Content).Source = newImage;

                // Calculate size based on saved size value
                double scale = crosshairs[index].SizeValue / 100.0;
                double newWidth = originalWidth * scale;
                double newHeight = originalHeight * scale;

                crosshairWindow.Width = newWidth;
                crosshairWindow.Height = newHeight;
                ((System.Windows.Controls.Image)crosshairWindow.Content).Width = newWidth;
                ((System.Windows.Controls.Image)crosshairWindow.Content).Height = newHeight;

                // Restore the position for this crosshair
                if (crosshairs[index].IsCentered)
                {
                    // Recalculate center position based on current size
                    double screenWidth = SystemParameters.PrimaryScreenWidth;
                    double screenHeight = SystemParameters.PrimaryScreenHeight;
                    crosshairWindow.Left = (screenWidth - crosshairWindow.Width) / 2;
                    crosshairWindow.Top = (screenHeight - crosshairWindow.Height) / 2;
                }
                else
                {
                    crosshairWindow.Left = crosshairs[index].Left;
                    crosshairWindow.Top = crosshairs[index].Top;
                }

                // Update position display
                UpdatePositionDisplay();
            }
        }

        private void SetHotkeyButton_Click(object sender, RoutedEventArgs e)
        {
            HotkeyTextBox.Text = "Press any key...";
            HotkeyTextBox.Focus();
        }

        private void MainWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == switchHotKey)
            {
                SwitchToNextCrosshair();
            }
        }

        private void SwitchToNextCrosshair()
        {
            do
            {
                currentCrosshairIndex = (currentCrosshairIndex + 1) % 3;
            } while (crosshairs[currentCrosshairIndex].Image == null &&
                    HasAnyCrosshairImage());

            if (crosshairs[currentCrosshairIndex].Image != null)
            {
                UpdateDisplayedCrosshair(currentCrosshairIndex);
            }
        }

        private bool HasAnyCrosshairImage()
        {
            return crosshairs.Any(c => c.Image != null);
        }

        private void RegisterHotKey()
        {
            // Unregister existing hotkey
            UnregisterHotKey(windowHandle, HOTKEY_ID);

            // Convert WPF Key to virtual key code
            uint vk = (uint)KeyInterop.VirtualKeyFromKey(switchHotKey);

            // Register new hotkey
            if (!RegisterHotKey(windowHandle, HOTKEY_ID, 0, vk))
            {
                System.Windows.MessageBox.Show("Failed to register hotkey. It might be in use by another application.",
                                  "Hotkey Registration Error",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            // Stop the timer
            if (topmostTimer != null)
            {
                topmostTimer.Stop();
                topmostTimer = null;
            }

            // Save settings before closing
            SaveSettings();

            // Unregister hotkey when closing
            UnregisterHotKey(windowHandle, HOTKEY_ID);

            // Close crosshair window
            if (crosshairWindow != null)
            {
                crosshairWindow.Close();
                crosshairWindow = null;
            }

            base.OnClosed(e);
        }

        private void SwitchTestButton_Click(object sender, RoutedEventArgs e)
        {
            SwitchToNextCrosshair();
        }

        private void SaveSettings()
        {
            try
            {
                settings.CrosshairPaths = crosshairs.Select(c => c.Path ?? "").ToArray();
                settings.LastUsedIndex = currentCrosshairIndex;
                settings.SizeValue = SizeSlider.Value;
                if (crosshairWindow != null)
                {
                    settings.WindowLeftPositions = new double[] { crosshairWindow.Left, crosshairWindow.Left, crosshairWindow.Left };
                    settings.WindowTopPositions = new double[] { crosshairWindow.Top, crosshairWindow.Top, crosshairWindow.Top };
                    settings.IsActive = crosshairWindow.Visibility == Visibility.Visible;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(settingsPath));
                using var writer = new StreamWriter(settingsPath);
                var serializer = new XmlSerializer(typeof(CrosshairSettings));
                serializer.Serialize(writer, settings);
            }
            catch { /* Ignore saving errors */ }
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(settingsPath))
                {
                    using var reader = new StreamReader(settingsPath);
                    var serializer = new XmlSerializer(typeof(CrosshairSettings));
                    settings = (CrosshairSettings)serializer.Deserialize(reader);

                    // Restore positions to crosshairs
                    for (int i = 0; i < crosshairs.Count; i++)
                    {
                        crosshairs[i].Left = settings.WindowLeftPositions[i];
                        crosshairs[i].Top = settings.WindowTopPositions[i];
                    }
                }
            }
            catch { /* Ignore loading errors */ }
        }

        private void ActivateDeactivateButton_Click(object sender, RoutedEventArgs e)
        {
            if (crosshairWindow != null)
            {
                crosshairWindow.Visibility = crosshairWindow.Visibility == Visibility.Visible
                    ? Visibility.Collapsed
                    : Visibility.Visible;
                settings.IsActive = crosshairWindow.Visibility == Visibility.Visible;

                if (crosshairWindow.Visibility == Visibility.Visible)
                {
                    EnsureTopMostOverlay();
                }

                // Update button text
                ActivateButton.Content = crosshairWindow.Visibility == Visibility.Visible
                    ? "Deactivate"
                    : "Activate";
            }
        }

        private void UpdatePositionDisplay()
        {
            if (crosshairWindow != null)
            {
                double screenCenterX = SystemParameters.PrimaryScreenWidth / 2;
                double screenCenterY = SystemParameters.PrimaryScreenHeight / 2;

                // Calculate relative position to screen center
                double relativeX = crosshairWindow.Left + (crosshairWindow.Width / 2) - screenCenterX;
                double relativeY = crosshairWindow.Top + (crosshairWindow.Height / 2) - screenCenterY;

                PositionXTextBox.Text = Math.Round(relativeX).ToString();
                PositionYTextBox.Text = Math.Round(relativeY).ToString();
                CurrentCrosshairText.Text = crosshairs[currentCrosshairIndex].Name;
            }
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            // Allow only digits and negative sign
            e.Handled = !int.TryParse(e.Text, out _) && e.Text != "-";
        }

        private void PositionTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (crosshairWindow == null || !double.TryParse(PositionXTextBox.Text, out double x) ||
                !double.TryParse(PositionYTextBox.Text, out double y))
                return;

            double screenCenterX = SystemParameters.PrimaryScreenWidth / 2;
            double screenCenterY = SystemParameters.PrimaryScreenHeight / 2;

            // Convert relative position to window position
            crosshairWindow.Left = screenCenterX - (crosshairWindow.Width / 2) + x;
            crosshairWindow.Top = screenCenterY - (crosshairWindow.Height / 2) + y;

            // Update stored position
            crosshairs[currentCrosshairIndex].Left = crosshairWindow.Left;
            crosshairs[currentCrosshairIndex].Top = crosshairWindow.Top;
            crosshairs[currentCrosshairIndex].IsCentered = false;
        }

        private void IncrementX_Click(object sender, RoutedEventArgs e)
        {
            if (crosshairWindow == null) return;
            UpdatePosition(1, 0);
        }

        private void DecrementX_Click(object sender, RoutedEventArgs e)
        {
            if (crosshairWindow == null) return;
            UpdatePosition(-1, 0);
        }

        private void IncrementY_Click(object sender, RoutedEventArgs e)
        {
            if (crosshairWindow == null) return;
            UpdatePosition(0, 1);
        }

        private void DecrementY_Click(object sender, RoutedEventArgs e)
        {
            if (crosshairWindow == null) return;
            UpdatePosition(0, -1);
        }

        private void UpdatePosition(int deltaX, int deltaY)
        {
            double screenCenterX = SystemParameters.PrimaryScreenWidth / 2;
            double screenCenterY = SystemParameters.PrimaryScreenHeight / 2;

            // Get current relative position
            double currentRelativeX = crosshairWindow.Left + (crosshairWindow.Width / 2) - screenCenterX;
            double currentRelativeY = crosshairWindow.Top + (crosshairWindow.Height / 2) - screenCenterY;

            // Update relative position
            double newRelativeX = currentRelativeX + deltaX;
            double newRelativeY = currentRelativeY + deltaY;

            // Convert back to window position
            crosshairWindow.Left = screenCenterX - (crosshairWindow.Width / 2) + newRelativeX;
            crosshairWindow.Top = screenCenterY - (crosshairWindow.Height / 2) + newRelativeY;

            // Update stored position
            crosshairs[currentCrosshairIndex].Left = crosshairWindow.Left;
            crosshairs[currentCrosshairIndex].Top = crosshairWindow.Top;
            crosshairs[currentCrosshairIndex].IsCentered = false;

            // Update display
            UpdatePositionDisplay();
        }

        private void EnsureTopmost()
        {
            if (crosshairWindow != null && crosshairWindow.Visibility == Visibility.Visible)
            {
                crosshairWindow.Topmost = false;
                crosshairWindow.Topmost = true;
            }
        }

        private void StartTopmostTimer()
        {
            topmostTimer = new DispatcherTimer();
            topmostTimer.Interval = TimeSpan.FromSeconds(25);
            topmostTimer.Tick += (s, e) => EnsureTopMostOverlay();
            topmostTimer.Start();
        }

        private void EnsureTopMostOverlay()
        {
            if (crosshairWindow != null && crosshairWindow.Visibility == Visibility.Visible)
            {
                IntPtr windowHandle = new WindowInteropHelper(crosshairWindow).Handle;
                SetWindowPos(windowHandle, HWND_TOPMOST, 0, 0, 0, 0, TOPMOST_FLAGS);

                // Refresh the window style to maintain click-through
                int exStyle = GetWindowLong(windowHandle, GWL_EXSTYLE);
                exStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT;
                SetWindowLong(windowHandle, GWL_EXSTYLE, exStyle);
            }
        }

        private BitmapImage GetDefaultCrosshairImage()
        {
            try
            {
                // Ensure default folder exists
                if (!Directory.Exists(settings.DefaultCrosshairFolder))
                {
                    Directory.CreateDirectory(settings.DefaultCrosshairFolder);
                    CreateDefaultCrosshairFile();
                }

                // Try to find any PNG file in the default folder
                var pngFiles = Directory.GetFiles(settings.DefaultCrosshairFolder, "*.png");
                if (pngFiles.Length > 0)
                {
                    return new BitmapImage(new Uri(pngFiles[0]));
                }

                // If no PNG files found, create and return a default crosshair
                string defaultCrosshairPath = CreateDefaultCrosshairFile();
                return new BitmapImage(new Uri(defaultCrosshairPath));
            }
            catch
            {
                // If all else fails, return a simple programmatically created crosshair
                return CreateProgrammaticCrosshair();
            }
        }

        private string CreateDefaultCrosshairFile()
        {
            string defaultPath = Path.Combine(settings.DefaultCrosshairFolder, "default_crosshair.png");
            
            try
            {
                // Create a simple crosshair bitmap programmatically
                var bitmap = CreateSimpleCrosshairBitmap();
                
                // Save to file
                using (var fileStream = new FileStream(defaultPath, FileMode.Create))
                {
                    BitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    encoder.Save(fileStream);
                }
                
                return defaultPath;
            }
            catch
            {
                return "";
            }
        }

        private BitmapSource CreateSimpleCrosshairBitmap()
        {
            int width = 32;
            int height = 32;
            int stride = width * 4; // 4 bytes per pixel (BGRA)
            byte[] pixels = new byte[height * stride];

            // Create a simple cross pattern
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * stride + x * 4;
                    
                    // Create cross pattern (vertical and horizontal lines)
                    if (x == width / 2 || y == height / 2)
                    {
                        pixels[index] = 255;     // Blue
                        pixels[index + 1] = 255; // Green  
                        pixels[index + 2] = 255; // Red
                        pixels[index + 3] = 255; // Alpha
                    }
                    else
                    {
                        pixels[index] = 0;       // Blue
                        pixels[index + 1] = 0;   // Green
                        pixels[index + 2] = 0;   // Red
                        pixels[index + 3] = 0;   // Alpha (transparent)
                    }
                }
            }

            return BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, pixels, stride);
        }

        private BitmapImage CreateProgrammaticCrosshair()
        {
            var bitmap = CreateSimpleCrosshairBitmap();
            
            // Convert BitmapSource to BitmapImage
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            
            using (var stream = new MemoryStream())
            {
                encoder.Save(stream);
                stream.Position = 0;
                
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = stream;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();
                
                return bitmapImage;
            }
        }

        private void UpdateDefaultFolderDisplay()
        {
            if (DefaultFolderTextBox != null)
            {
                DefaultFolderTextBox.Text = settings.DefaultCrosshairFolder;
            }
        }

        private void ChangeDefaultFolderButton_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new WinForms.FolderBrowserDialog())
            {
                dialog.Description = "Select Default Crosshair Folder";
                dialog.SelectedPath = settings.DefaultCrosshairFolder;
                dialog.ShowNewFolderButton = true;

                if (dialog.ShowDialog() == WinForms.DialogResult.OK)
                {
                    settings.DefaultCrosshairFolder = dialog.SelectedPath;
                    UpdateDefaultFolderDisplay();
                    SaveSettings();
                    
                    System.Windows.MessageBox.Show($"Default crosshair folder changed to:\n{settings.DefaultCrosshairFolder}", 
                                  "Folder Changed", 
                                  MessageBoxButton.OK, 
                                  MessageBoxImage.Information);
                }
            }
        }

        private void OpenDefaultFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Ensure the folder exists
                if (!Directory.Exists(settings.DefaultCrosshairFolder))
                {
                    Directory.CreateDirectory(settings.DefaultCrosshairFolder);
                }

                // Open the folder in Windows Explorer
                System.Diagnostics.Process.Start("explorer.exe", settings.DefaultCrosshairFolder);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Could not open folder:\n{ex.Message}", 
                              "Error", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
            }
        }
    }

    public class CrosshairSettings
    {
        public string[] CrosshairPaths { get; set; } = new string[3];
        public int LastUsedIndex { get; set; } = 0;
        public double[] WindowLeftPositions { get; set; } = new double[3];
        public double[] WindowTopPositions { get; set; } = new double[3];
        public double SizeValue { get; set; } = 100;
        public bool IsActive { get; set; } = true;
        public string DefaultCrosshairFolder { get; set; } = 
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Crosshairs");
    }

    public static class ProgramInfo
    {
        public const string VERSION = "V3.1";
        public const string EDITION = "Founder Deluxe Edition";
        public const string FULL_NAME = $"SwitchScope Crosshair {VERSION}";
        public const string FULL_TITLE = $"SwitchScope Crosshair {VERSION} {EDITION}";
    }
}