using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MySql.Data.MySqlClient;
using System.IO;

namespace zadanie2
{
    public partial class MainWindow : Window
    {
        private string connectionString = "Server=85.236.189.123;Port=13306;Database=isp-44_anufriev_va_zadanie_2;Uid=isp-44_anufriev_va;Pwd=isp-44_anufriev_va;CharSet=utf8mb4;";
        private List<Service> allServices = new List<Service>();
        private string searchText = "";
        private string sortField = "Title";
        private bool sortAsc = true;
        private bool _ready = false;

        public MainWindow()
        {
            InitializeComponent();
            _ready = true;
            LoadServices();
        }

        private void LoadServices()
        {
            allServices.Clear();
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    var cmd = new MySqlCommand("SELECT ID, Title, Cost, DurationInSeconds, Discount, MainImagePath FROM Service", conn);
                    var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        allServices.Add(new Service
                        {
                            ID = reader.GetInt32("ID"),
                            Title = reader.GetString("Title"),
                            Cost = reader.GetDecimal("Cost"),
                            DurationInSeconds = reader.GetInt32("DurationInSeconds"),
                            Discount = reader.IsDBNull(reader.GetOrdinal("Discount")) ? (double?)null : reader.GetDouble("Discount"),
                            MainImagePath = reader.IsDBNull(reader.GetOrdinal("MainImagePath")) ? null : reader.GetString("MainImagePath")
                        });
                    }
                }
                lblCount.Text = $"Всего: {allServices.Count}";
                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения к базе данных:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilter()
        {
            if (!_ready || ServicesPanel == null) return;
            var filtered = new List<Service>();
            foreach (var s in allServices)
            {
                if (string.IsNullOrEmpty(searchText) || s.Title.ToLower().Contains(searchText.ToLower()))
                    filtered.Add(s);
            }

            if (sortField == "Title")
                filtered.Sort((a, b) => sortAsc ? string.Compare(a.Title, b.Title) : string.Compare(b.Title, a.Title));
            else if (sortField == "Cost")
                filtered.Sort((a, b) => sortAsc ? a.Cost.CompareTo(b.Cost) : b.Cost.CompareTo(a.Cost));
            else if (sortField == "Duration")
                filtered.Sort((a, b) => sortAsc ? a.DurationInSeconds.CompareTo(b.DurationInSeconds) : b.DurationInSeconds.CompareTo(a.DurationInSeconds));

            lblCount.Text = $"Показано: {filtered.Count} из {allServices.Count}";
            RenderServices(filtered);
        }

        private void RenderServices(List<Service> services)
        {
            if (ServicesPanel == null) return;
            ServicesPanel.Children.Clear();
            foreach (var s in services)
            {
                var border = new Border
                {
                    BorderBrush = Brushes.LightGray,
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(5),
                    Padding = new Thickness(8),
                    Background = Brushes.White,
                    CornerRadius = new CornerRadius(4),
                    Width = 760
                };

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var imgBorder = new Border
                {
                    Width = 90,
                    Height = 90,
                    Margin = new Thickness(0, 0, 10, 0),
                    Background = s.Discount.HasValue ? new SolidColorBrush(Color.FromRgb(201, 207, 230)) : new SolidColorBrush(Color.FromRgb(230, 207, 207))
                };

                if (!string.IsNullOrEmpty(s.MainImagePath))
                {
                    // DB stores paths like "Услуги школы\Китайский язык.jpg"
                    // images are in Images\ subfolder of output dir
                    string relPath = s.MainImagePath.Replace("\\", Path.DirectorySeparatorChar.ToString());
                    string imgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", relPath);
                    if (File.Exists(imgPath))
                    {
                        try
                        {
                            var bmp = new BitmapImage();
                            bmp.BeginInit();
                            bmp.UriSource = new Uri(imgPath);
                            bmp.CacheOption = BitmapCacheOption.OnLoad;
                            bmp.EndInit();
                            var img = new Image { Source = bmp, Stretch = Stretch.UniformToFill };
                            imgBorder.Child = img;
                        }
                        catch { }
                    }
                }

                Grid.SetColumn(imgBorder, 0);
                grid.Children.Add(imgBorder);

                var infoPanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

                var titleText = new TextBlock
                {
                    Text = s.Title,
                    FontSize = 14,
                    FontWeight = FontWeights.SemiBold,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 4)
                };
                infoPanel.Children.Add(titleText);

                var pricePanel = new StackPanel { Orientation = Orientation.Horizontal };
                if (s.Discount.HasValue && s.Discount > 0)
                {
                    decimal discounted = s.Cost * (1 - (decimal)(s.Discount.Value / 100.0));
                    var origPrice = new TextBlock
                    {
                        Text = $"{s.Cost:0} рублей",
                        TextDecorations = TextDecorations.Strikethrough,
                        Foreground = Brushes.Gray,
                        FontSize = 13,
                        Margin = new Thickness(0, 0, 6, 0)
                    };
                    var newPrice = new TextBlock
                    {
                        Text = $"{discounted:0} рублей за {FormatDuration(s.DurationInSeconds)}",
                        FontSize = 13,
                        FontWeight = FontWeights.SemiBold
                    };
                    pricePanel.Children.Add(origPrice);
                    pricePanel.Children.Add(newPrice);
                    infoPanel.Children.Add(pricePanel);

                    var discLabel = new TextBlock
                    {
                        Text = $"* скидка {s.Discount}%",
                        Foreground = Brushes.Gray,
                        FontSize = 11,
                        Margin = new Thickness(0, 2, 0, 0)
                    };
                    infoPanel.Children.Add(discLabel);
                }
                else
                {
                    var priceText = new TextBlock
                    {
                        Text = $"{s.Cost:0} рублей за {FormatDuration(s.DurationInSeconds)}",
                        FontSize = 13
                    };
                    pricePanel.Children.Add(priceText);
                    infoPanel.Children.Add(pricePanel);
                }

                var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };

                var editBtn = new Button
                {
                    Content = "редактировать",
                    Padding = new Thickness(10, 4, 10, 4),
                    Margin = new Thickness(0, 0, 8, 0),
                    Background = Brushes.White,
                    BorderBrush = Brushes.LightGray,
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                editBtn.Tag = s;
                editBtn.Click += EditBtn_Click;

                var delBtn = new Button
                {
                    Content = "удалить",
                    Padding = new Thickness(10, 4, 10, 4),
                    Background = Brushes.White,
                    BorderBrush = Brushes.LightGray,
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                delBtn.Tag = s;
                delBtn.Click += DeleteBtn_Click;

                btnPanel.Children.Add(editBtn);
                btnPanel.Children.Add(delBtn);
                infoPanel.Children.Add(btnPanel);

                Grid.SetColumn(infoPanel, 1);
                grid.Children.Add(infoPanel);
                border.Child = grid;
                ServicesPanel.Children.Add(border);
            }
        }

        private string FormatDuration(int seconds)
        {
            int hours = seconds / 3600;
            int minutes = (seconds % 3600) / 60;
            if (hours > 0 && minutes > 0)
                return $"{hours} ч {minutes} мин";
            else if (hours > 0)
                return $"{hours} ч";
            else
                return $"{minutes} мин";
        }

        private void EditBtn_Click(object sender, RoutedEventArgs e)
        {
            var s = (Service)((Button)sender).Tag;
            var editWin = new EditServiceWindow(connectionString, s);
            if (editWin.ShowDialog() == true)
                LoadServices();
        }

        private void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            var s = (Service)((Button)sender).Tag;
            var result = MessageBox.Show($"Удалить услугу \"{s.Title}\"?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();
                        var cmd = new MySqlCommand("DELETE FROM Service WHERE ID = @id", conn);
                        cmd.Parameters.AddWithValue("@id", s.ID);
                        cmd.ExecuteNonQuery();
                    }
                    LoadServices();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            var editWin = new EditServiceWindow(connectionString, null);
            if (editWin.ShowDialog() == true)
                LoadServices();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_ready) return;
            searchText = SearchBox.Text;
            ApplyFilter();
        }

        private void SortCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_ready) return;
            if (SortCombo.SelectedIndex == 0) sortField = "Title";
            else if (SortCombo.SelectedIndex == 1) sortField = "Cost";
            else if (SortCombo.SelectedIndex == 2) sortField = "Duration";
            ApplyFilter();
        }

        private void SortDirBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!_ready) return;
            sortAsc = !sortAsc;
            SortDirBtn.Content = sortAsc ? "↑" : "↓";
            ApplyFilter();
        }
    }
}
