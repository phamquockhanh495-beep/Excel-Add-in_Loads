using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace QksExcelAddIn.Services
{
    public class CustomMessageBox : Window
    {
        public enum IconType
        {
            Success,
            Warning,
            ErrorType,
            Question
        }

        private TextBox? _txtInput;
        public string InputValue => _txtInput?.Text?.Trim() ?? string.Empty;

        public CustomMessageBox(string prompt, string title, string defaultValue, bool isInput = true)
        {
            Width = 460;
            SizeToContent = SizeToContent.Height;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Topmost = true;
            ShowInTaskbar = false;
            FontFamily = new FontFamily("Segoe UI");
            FontSize = 12;
            Background = Brushes.White;
            BorderBrush = new SolidColorBrush(Color.FromRgb(15, 34, 64)); // Navy #0F2240
            BorderThickness = new Thickness(1);

            var rootGrid = new Grid();
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(34) });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // 1. Headbar
            var headbar = new Grid { Background = new SolidColorBrush(Color.FromRgb(15, 34, 64)) };
            headbar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headbar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headbar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(35) });

            try
            {
                var logoImg = new Image
                {
                    Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/QksExcelAddIn;component/Resources/excel.png", UriKind.Absolute)),
                    Width = 18,
                    Height = 18,
                    Margin = new Thickness(10, 0, 6, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(logoImg, 0);
                headbar.Children.Add(logoImg);
                Icon = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/QksExcelAddIn;component/Resources/excel.ico", UriKind.Absolute));
            }
            catch { }

            var titleBlock = new TextBlock
            {
                Text = title,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 11.5,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(2, 0, 0, 0)
            };
            Grid.SetColumn(titleBlock, 1);
            headbar.Children.Add(titleBlock);

            var closeBtn = new Button
            {
                Content = "✕",
                Foreground = Brushes.White,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                FontWeight = FontWeights.Bold,
                FontSize = 11,
                Cursor = Cursors.Hand,
                Focusable = false
            };
            var closeBtnTemplate = new ControlTemplate(typeof(Button));
            var closeBorderFactory = new FrameworkElementFactory(typeof(Border));
            closeBorderFactory.Name = "border";
            closeBorderFactory.SetValue(Border.BackgroundProperty, Brushes.Transparent);
            var closeContentFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            closeContentFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            closeContentFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            closeBorderFactory.AppendChild(closeContentFactory);
            closeBtnTemplate.VisualTree = closeBorderFactory;

            var closeHoverTrigger = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
            closeHoverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(220, 50, 50)), "border"));
            closeBtnTemplate.Triggers.Add(closeHoverTrigger);
            closeBtn.Template = closeBtnTemplate;
            closeBtn.Click += (s, e) => { DialogResult = false; Close(); };
            Grid.SetColumn(closeBtn, 2);
            headbar.Children.Add(closeBtn);

            headbar.MouseDown += (s, e) =>
            {
                if (e.ChangedButton == MouseButton.Left) DragMove();
            };

            Grid.SetRow(headbar, 0);
            rootGrid.Children.Add(headbar);

            // 2. Main Content
            var contentGrid = new Grid { Margin = new Thickness(18, 14, 18, 14) };
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(46) });
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var iconVisual = CreateIconVisual(IconType.Question);
            Grid.SetColumn(iconVisual, 0);
            contentGrid.Children.Add(iconVisual);

            var rightPanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0) };
            var messageBlock = new TextBlock
            {
                Text = prompt,
                Foreground = new SolidColorBrush(Color.FromRgb(15, 34, 64)),
                FontSize = 12,
                FontWeight = FontWeights.Medium,
                Margin = new Thickness(0, 0, 0, 8),
                TextWrapping = TextWrapping.Wrap
            };
            rightPanel.Children.Add(messageBlock);

            _txtInput = new TextBox
            {
                Text = defaultValue ?? "",
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 12,
                Height = 28,
                Padding = new Thickness(6, 4, 6, 4),
                BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                BorderThickness = new Thickness(1),
                VerticalContentAlignment = VerticalAlignment.Center
            };
            _txtInput.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    DialogResult = true;
                    Close();
                }
            };
            rightPanel.Children.Add(_txtInput);

            Grid.SetColumn(rightPanel, 1);
            contentGrid.Children.Add(rightPanel);

            Grid.SetRow(contentGrid, 1);
            rootGrid.Children.Add(contentGrid);

            // 3. Footer
            var footer = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(245, 246, 248)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(12, 6, 12, 6)
            };

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var cancelBtn = new Button
            {
                Content = "HỦY BỎ",
                Width = 80,
                Height = 28,
                Margin = new Thickness(0, 0, 8, 0),
                Foreground = new SolidColorBrush(Color.FromRgb(55, 65, 81)),
                Background = new SolidColorBrush(Color.FromRgb(229, 231, 235)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(209, 213, 219)),
                BorderThickness = new Thickness(1),
                FontWeight = FontWeights.SemiBold,
                FontSize = 11,
                Cursor = Cursors.Hand
            };
            cancelBtn.Click += (s, e) => { DialogResult = false; Close(); };
            buttonPanel.Children.Add(cancelBtn);

            var confirmBtn = new Button
            {
                Content = "XÁC NHẬN",
                Width = 90,
                Height = 28,
                Foreground = Brushes.White,
                Background = new SolidColorBrush(Color.FromRgb(15, 34, 64)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(15, 34, 64)),
                BorderThickness = new Thickness(1),
                FontWeight = FontWeights.Bold,
                FontSize = 11,
                Cursor = Cursors.Hand,
                IsDefault = true
            };
            confirmBtn.Click += (s, e) => { DialogResult = true; Close(); };
            buttonPanel.Children.Add(confirmBtn);

            footer.Child = buttonPanel;
            Grid.SetRow(footer, 2);
            rootGrid.Children.Add(footer);

            Content = rootGrid;

            Loaded += (s, e) =>
            {
                _txtInput.Focus();
                _txtInput.SelectAll();
            };

            KeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape)
                {
                    DialogResult = false;
                    Close();
                }
            };
        }

        public CustomMessageBox(string message, string title, IconType icon, bool isConfirm = false)
        {
            Width = 460;
            SizeToContent = SizeToContent.Height;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Topmost = true;
            ShowInTaskbar = false;
            FontFamily = new FontFamily("Segoe UI");
            FontSize = 12;
            Background = Brushes.White;
            BorderBrush = new SolidColorBrush(Color.FromRgb(15, 34, 64)); // Navy #0F2240
            BorderThickness = new Thickness(1);

            // Root Grid
            var rootGrid = new Grid();
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(34) }); // Headbar
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Content
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Footer

            // 1. Headbar
            var headbar = new Grid
            {
                Background = new SolidColorBrush(Color.FromRgb(15, 34, 64))
            };
            headbar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headbar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headbar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(35) });

            try
            {
                var logoImg = new Image
                {
                    Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/QksExcelAddIn;component/Resources/excel.png", UriKind.Absolute)),
                    Width = 18,
                    Height = 18,
                    Margin = new Thickness(10, 0, 6, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(logoImg, 0);
                headbar.Children.Add(logoImg);

                Icon = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/QksExcelAddIn;component/Resources/excel.ico", UriKind.Absolute));
            }
            catch { }

            var titleBlock = new TextBlock
            {
                Text = title,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 11.5,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(2, 0, 0, 0)
            };
            Grid.SetColumn(titleBlock, 1);
            headbar.Children.Add(titleBlock);

            var closeBtn = new Button
            {
                Content = "✕",
                Foreground = Brushes.White,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                FontWeight = FontWeights.Bold,
                FontSize = 11,
                Cursor = Cursors.Hand,
                Focusable = false
            };
            var closeBtnTemplate = new ControlTemplate(typeof(Button));
            var closeBorderFactory = new FrameworkElementFactory(typeof(Border));
            closeBorderFactory.Name = "border";
            closeBorderFactory.SetValue(Border.BackgroundProperty, Brushes.Transparent);
            var closeContentFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            closeContentFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            closeContentFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            closeBorderFactory.AppendChild(closeContentFactory);
            closeBtnTemplate.VisualTree = closeBorderFactory;

            var closeHoverTrigger = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
            closeHoverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(220, 50, 50)), "border"));
            closeBtnTemplate.Triggers.Add(closeHoverTrigger);
            closeBtn.Template = closeBtnTemplate;
            closeBtn.Click += (s, e) => { DialogResult = false; Close(); };
            Grid.SetColumn(closeBtn, 2);
            headbar.Children.Add(closeBtn);

            headbar.MouseDown += (s, e) =>
            {
                if (e.ChangedButton == MouseButton.Left)
                {
                    DragMove();
                }
            };

            Grid.SetRow(headbar, 0);
            rootGrid.Children.Add(headbar);

            // 2. Main Content
            var contentGrid = new Grid
            {
                Margin = new Thickness(18, 14, 18, 14)
            };
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(46) });
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var iconVisual = CreateIconVisual(icon);
            Grid.SetColumn(iconVisual, 0);
            contentGrid.Children.Add(iconVisual);

            var messageBlock = new TextBlock
            {
                Text = message,
                Foreground = new SolidColorBrush(Color.FromRgb(15, 34, 64)),
                FontSize = 12,
                LineHeight = 18,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 0, 0)
            };
            Grid.SetColumn(messageBlock, 1);
            contentGrid.Children.Add(messageBlock);

            Grid.SetRow(contentGrid, 1);
            rootGrid.Children.Add(contentGrid);

            // 3. Footer
            var footer = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(245, 246, 248)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(12, 6, 12, 6)
            };

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            if (isConfirm)
            {
                var cancelBtn = new Button
                {
                    Content = "HỦY BỎ",
                    Width = 80,
                    Height = 28,
                    Margin = new Thickness(0, 0, 8, 0),
                    Foreground = new SolidColorBrush(Color.FromRgb(55, 65, 81)),
                    Background = new SolidColorBrush(Color.FromRgb(229, 231, 235)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(209, 213, 219)),
                    BorderThickness = new Thickness(1),
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 11,
                    Cursor = Cursors.Hand
                };
                cancelBtn.Click += (s, e) => { DialogResult = false; Close(); };
                buttonPanel.Children.Add(cancelBtn);

                var confirmBtn = new Button
                {
                    Content = "XÁC NHẬN",
                    Width = 90,
                    Height = 28,
                    Foreground = Brushes.White,
                    Background = new SolidColorBrush(Color.FromRgb(15, 34, 64)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(15, 34, 64)),
                    BorderThickness = new Thickness(1),
                    FontWeight = FontWeights.Bold,
                    FontSize = 11,
                    Cursor = Cursors.Hand,
                    IsDefault = true
                };
                confirmBtn.Click += (s, e) => { DialogResult = true; Close(); };
                buttonPanel.Children.Add(confirmBtn);
            }
            else
            {
                var okButton = new Button
                {
                    Content = "ĐỒNG Ý",
                    Width = 85,
                    Height = 28,
                    Foreground = Brushes.White,
                    Background = new SolidColorBrush(Color.FromRgb(15, 34, 64)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(15, 34, 64)),
                    BorderThickness = new Thickness(1),
                    FontWeight = FontWeights.Bold,
                    FontSize = 11,
                    Cursor = Cursors.Hand,
                    IsDefault = true
                };

                var okBtnTemplate = new ControlTemplate(typeof(Button));
                var okBorderFactory = new FrameworkElementFactory(typeof(Border));
                okBorderFactory.Name = "border";
                okBorderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
                okBorderFactory.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Button.BorderBrushProperty));
                okBorderFactory.SetValue(Border.BorderThicknessProperty, new Thickness(1));
                okBorderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(3));

                var okContentFactory = new FrameworkElementFactory(typeof(ContentPresenter));
                okContentFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
                okContentFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
                okBorderFactory.AppendChild(okContentFactory);
                okBtnTemplate.VisualTree = okBorderFactory;

                var okHoverTrigger = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
                okHoverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(30, 65, 120)), "border"));
                okHoverTrigger.Setters.Add(new Setter(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(30, 65, 120)), "border"));
                okBtnTemplate.Triggers.Add(okHoverTrigger);
                okButton.Template = okBtnTemplate;

                okButton.Click += (s, e) => { DialogResult = true; Close(); };
                buttonPanel.Children.Add(okButton);
            }

            footer.Child = buttonPanel;
            Grid.SetRow(footer, 2);
            rootGrid.Children.Add(footer);

            Content = rootGrid;

            KeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape)
                {
                    DialogResult = false;
                    Close();
                }
            };
        }

        private static FrameworkElement CreateIconVisual(IconType icon)
        {
            var grid = new Grid
            {
                Width = 36,
                Height = 36,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var ellipse = new Ellipse
            {
                Width = 36,
                Height = 36
            };
            grid.Children.Add(ellipse);

            switch (icon)
            {
                case IconType.Success:
                    ellipse.Fill = new SolidColorBrush(Color.FromRgb(16, 185, 129)); // #10B981
                    var checkText = new TextBlock
                    {
                        Text = "✓",
                        Foreground = Brushes.White,
                        FontSize = 20,
                        FontWeight = FontWeights.Bold,
                        FontFamily = new FontFamily("Segoe UI"),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, -2, 0, 0)
                    };
                    grid.Children.Add(checkText);
                    break;

                case IconType.Warning:
                    ellipse.Fill = new SolidColorBrush(Color.FromRgb(245, 158, 11)); // #F59E0B
                    var warnText = new TextBlock
                    {
                        Text = "!",
                        Foreground = Brushes.White,
                        FontSize = 22,
                        FontWeight = FontWeights.Bold,
                        FontFamily = new FontFamily("Segoe UI"),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, -2, 0, 0)
                    };
                    grid.Children.Add(warnText);
                    break;

                case IconType.ErrorType:
                    ellipse.Fill = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // #EF4444
                    var errText = new TextBlock
                    {
                        Text = "✕",
                        Foreground = Brushes.White,
                        FontSize = 18,
                        FontWeight = FontWeights.Bold,
                        FontFamily = new FontFamily("Segoe UI"),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, -1, 0, 0)
                    };
                    grid.Children.Add(errText);
                    break;

                case IconType.Question:
                    ellipse.Fill = new SolidColorBrush(Color.FromRgb(37, 99, 235)); // #2563EB Blue
                    var qText = new TextBlock
                    {
                        Text = "?",
                        Foreground = Brushes.White,
                        FontSize = 22,
                        FontWeight = FontWeights.Bold,
                        FontFamily = new FontFamily("Segoe UI"),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, -2, 0, 0)
                    };
                    grid.Children.Add(qText);
                    break;
            }

            return grid;
        }

        public static bool? Show(string message, string title, IconType icon)
        {
            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
            {
                return ShowInternal(message, title, icon, false);
            }
            else
            {
                bool? result = null;
                var staThread = new Thread(() =>
                {
                    result = ShowInternal(message, title, icon, false);
                });
                staThread.SetApartmentState(ApartmentState.STA);
                staThread.IsBackground = true;
                staThread.Start();
                staThread.Join();
                return result;
            }
        }

        public static bool? ShowConfirm(string message, string title = "Xác nhận")
        {
            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
            {
                return ShowInternal(message, title, IconType.Question, true);
            }
            else
            {
                bool? result = null;
                var staThread = new Thread(() =>
                {
                    result = ShowInternal(message, title, IconType.Question, true);
                });
                staThread.SetApartmentState(ApartmentState.STA);
                staThread.IsBackground = true;
                staThread.Start();
                staThread.Join();
                return result;
            }
        }

        private static bool? ShowInternal(string message, string title, IconType icon, bool isConfirm)
        {
            try
            {
                var box = new CustomMessageBox(message, title, icon, isConfirm);
                try
                {
                    if (Application.Current != null && Application.Current.MainWindow != null && Application.Current.MainWindow.IsVisible)
                    {
                        box.Owner = Application.Current.MainWindow;
                    }
                }
                catch { }

                return box.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi hiển thị CustomMessageBox: {ex.Message}");
                var btn = isConfirm ? MessageBoxButton.OKCancel : MessageBoxButton.OK;
                return MessageBox.Show(message, title, btn) == MessageBoxResult.OK;
            }
        }

        public static bool? Show(string message, string title = "Thông báo", bool isSuccess = false)
        {
            return Show(message, title, isSuccess ? IconType.Success : IconType.Warning);
        }

        public static string? ShowInput(string prompt, string title = "Nhập thông tin", string defaultValue = "")
        {
            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
            {
                var box = new CustomMessageBox(prompt, title, defaultValue, isInput: true);
                try
                {
                    if (Application.Current != null && Application.Current.MainWindow != null && Application.Current.MainWindow.IsVisible)
                    {
                        box.Owner = Application.Current.MainWindow;
                    }
                }
                catch { }
                bool? res = box.ShowDialog();
                return res == true ? box.InputValue : null;
            }
            else
            {
                string? result = null;
                var staThread = new Thread(() =>
                {
                    var box = new CustomMessageBox(prompt, title, defaultValue, isInput: true);
                    try
                    {
                        if (Application.Current != null && Application.Current.MainWindow != null && Application.Current.MainWindow.IsVisible)
                        {
                            box.Owner = Application.Current.MainWindow;
                        }
                    }
                    catch { }
                    bool? res = box.ShowDialog();
                    result = res == true ? box.InputValue : null;
                });
                staThread.SetApartmentState(ApartmentState.STA);
                staThread.IsBackground = true;
                staThread.Start();
                staThread.Join();
                return result;
            }
        }
    }
}

