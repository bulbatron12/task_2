using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using MySql.Data.MySqlClient;
using Microsoft.Win32;
using System.IO;

namespace zadanie2
{
    public partial class EditServiceWindow : Window
    {
        private string connectionString;
        private Service service;
        private bool isNew;
        private string selectedImagePath;

        public EditServiceWindow(string connStr, Service svc)
        {
            InitializeComponent();
            connectionString = connStr;
            service = svc;
            isNew = svc == null;

            if (!isNew)
            {
                txtTitle.Text = service.Title;
                txtCost.Text = service.Cost.ToString("0.00");
                txtDuration.Text = service.DurationInSeconds.ToString();
                txtDiscount.Text = service.Discount.HasValue ? service.Discount.Value.ToString() : "";
                txtDescription.Text = service.Description ?? "";
                selectedImagePath = service.MainImagePath;
                if (!string.IsNullOrEmpty(selectedImagePath))
                    lblImagePath.Text = selectedImagePath;
            }

            Title = isNew ? "Добавить услугу" : "Редактировать услугу";
        }

        private void BtnChooseImage_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp;*.gif|Все файлы|*.*",
                Title = "Выберите изображение"
            };
            if (dlg.ShowDialog() == true)
            {
                selectedImagePath = dlg.FileName;
                lblImagePath.Text = Path.GetFileName(selectedImagePath);
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTitle.Text))
            {
                MessageBox.Show("Введите название услуги.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(txtCost.Text.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal cost) || cost < 0)
            {
                MessageBox.Show("Введите корректную цену.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(txtDuration.Text, out int duration) || duration <= 0)
            {
                MessageBox.Show("Введите корректную длительность в секундах.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            double? discount = null;
            if (!string.IsNullOrWhiteSpace(txtDiscount.Text))
            {
                if (!double.TryParse(txtDiscount.Text.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double d) || d < 0 || d > 100)
                {
                    MessageBox.Show("Скидка должна быть числом от 0 до 100.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                discount = d;
            }

            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    MySqlCommand cmd;
                    if (isNew)
                    {
                        cmd = new MySqlCommand(
                            "INSERT INTO Service (Title, Cost, DurationInSeconds, Discount, Description, MainImagePath) VALUES (@t, @c, @d, @disc, @desc, @img)",
                            conn);
                    }
                    else
                    {
                        cmd = new MySqlCommand(
                            "UPDATE Service SET Title=@t, Cost=@c, DurationInSeconds=@d, Discount=@disc, Description=@desc, MainImagePath=@img WHERE ID=@id",
                            conn);
                        cmd.Parameters.AddWithValue("@id", service.ID);
                    }
                    cmd.Parameters.AddWithValue("@t", txtTitle.Text.Trim());
                    cmd.Parameters.AddWithValue("@c", cost);
                    cmd.Parameters.AddWithValue("@d", duration);
                    cmd.Parameters.AddWithValue("@disc", (object)discount ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@desc", string.IsNullOrWhiteSpace(txtDescription.Text) ? (object)DBNull.Value : txtDescription.Text.Trim());
                    cmd.Parameters.AddWithValue("@img", string.IsNullOrEmpty(selectedImagePath) ? (object)DBNull.Value : selectedImagePath);
                    cmd.ExecuteNonQuery();
                }
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
