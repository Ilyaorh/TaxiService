using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TaxiService.Business.Modules;
using TaxiService.Data.Models;

namespace TaxiService.WPF
{
    public partial class MainWindow : Window
    {
        private readonly TaxiRideModule _module;

        public MainWindow()
        {
            InitializeComponent();
            _module = new TaxiRideModule();
            LoadInitialData();
        }

        private void LoadInitialData()
        {
            try
            {
                var users = _module.GetAllUsers();
                cbUsers.ItemsSource = users;
                cbUsers.DisplayMemberPath = "FullName";
                cbUsers.SelectedValuePath = "UserID";

                cbTripUsers.ItemsSource = users.ToList();
                cbTripUsers.DisplayMemberPath = "FullName";
                cbTripUsers.SelectedValuePath = "UserID";

                var tariffs = _module.GetAllTariffs();
                cbTariffs.ItemsSource = tariffs;
                cbTariffs.DisplayMemberPath = "Name";
                cbTariffs.SelectedValuePath = "TariffID";

                LoadDrivers();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadDrivers()
        {
            var drivers = _module.GetAllDrivers();
            cbDrivers.ItemsSource = drivers;
            cbDrivers.DisplayMemberPath = "FullName";
            cbDrivers.SelectedValuePath = "DriverID";

            icDrivers.ItemsSource = _module.GetAvailableDrivers();
        }

        private void CbTariffs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CalculateCost();
        }

        private void TxtDistance_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalculateCost();
        }

        private void CalculateCost()
        {
            if (cbTariffs.SelectedItem is Tariff selectedTariff &&
                decimal.TryParse(txtDistance.Text, out decimal distance))
            {
                try
                {
                    var totalCost = _module.CalculateTripCost(distance, selectedTariff.TariffID);
                    var commission = _module.CalculateServiceCommission(totalCost, selectedTariff.TariffID);

                    txtTariffInfo.Text = selectedTariff.Name;
                    txtBasePrice.Text = $"📍 Базовая стоимость: {selectedTariff.BasePrice} ₽";
                    txtPricePerKm.Text = $"📏 Цена за км: {selectedTariff.PricePerKm} ₽/км";

                    txtCalculationDetails.Text =
                        $"Формула расчёта:\n" +
                        $"{selectedTariff.BasePrice} + ({distance} км × {selectedTariff.PricePerKm} ₽) = {totalCost} ₽\n\n" +
                        $"Комиссия сервиса ({selectedTariff.CommissionPercent}%): {commission} ₽";

                    txtTotalCost.Text = $"{totalCost} ₽";
                    txtCommission.Text = $"В т.ч. комиссия сервиса: {commission} ₽";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка расчёта: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnCreateTrip_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cbUsers.SelectedItem == null)
                {
                    MessageBox.Show("❌ Выберите пассажира", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (cbTariffs.SelectedItem == null)
                {
                    MessageBox.Show("❌ Выберите тариф", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtStartAddress.Text) ||
                    string.IsNullOrWhiteSpace(txtEndAddress.Text))
                {
                    MessageBox.Show("❌ Введите адреса", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!decimal.TryParse(txtDistance.Text, out decimal distance) || distance <= 0)
                {
                    MessageBox.Show("❌ Введите корректное расстояние", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int userId = (int)cbUsers.SelectedValue;
                int tariffId = (int)cbTariffs.SelectedValue;
                int? driverId = cbDrivers.SelectedValue as int?;

                var trip = _module.CreateTrip(
                    userId, driverId, tariffId,
                    txtStartAddress.Text, txtEndAddress.Text, distance);

                MessageBox.Show($"✅ Поездка №{trip.TripID} создана!\nСтоимость: {trip.TotalCost} ₽",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                txtStartAddress.Clear();
                txtEndAddress.Clear();
                txtDistance.Clear();
                cbDrivers.SelectedIndex = -1;

                LoadDrivers();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnRefreshDrivers_Click(object sender, RoutedEventArgs e)
        {
            LoadDrivers();
        }

        private void BtnChangeDriverStatus_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int driverId)
            {
                var dialog = new StatusChangeDialog(new[] { "Available", "Busy", "Offline" });
                dialog.Owner = this;

                if (dialog.ShowDialog() == true)
                {
                    _module.UpdateDriverStatus(driverId, dialog.SelectedStatus);
                    LoadDrivers();
                }
            }
        }

        private void CbTripUsers_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

        private void BtnShowTrips_Click(object sender, RoutedEventArgs e)
        {
            if (cbTripUsers.SelectedItem is User selectedUser)
            {
                var trips = _module.GetUserTripHistory(selectedUser.UserID);

                var tripsWithRoute = trips.Select(t => new
                {
                    t.TripID,
                    t.CreatedAt,
                    Route = $"{t.StartAddress} → {t.EndAddress}",
                    DriverName = t.Driver?.FullName ?? "Не назначен",
                    TariffName = t.Tariff?.Name ?? "Не указан",
                    t.DistanceKm,
                    t.TotalCost,
                    t.Status
                }).ToList();

                dgTrips.ItemsSource = tripsWithRoute;
            }
        }

        private void DgTrips_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

        private void BtnPay_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!int.TryParse(txtPaymentTripId.Text, out int tripId))
                {
                    MessageBox.Show("❌ Введите номер поездки", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!decimal.TryParse(txtPaymentAmount.Text, out decimal amount) || amount <= 0)
                {
                    MessageBox.Show("❌ Введите сумму", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string method = (cbPaymentMethod.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Cash";

                var payment = _module.CreatePayment(tripId, amount, method);
                _module.CompleteTrip(tripId);

                string receipt = _module.GenerateReceipt(tripId);

                txtPaymentResult.Text = $"✅ Оплата прошла успешно!\nПлатёж №{payment.PaymentID}";

                MessageBox.Show(receipt, "📄 Квитанция", MessageBoxButton.OK, MessageBoxImage.Information);

                txtPaymentTripId.Clear();
                txtPaymentAmount.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _module?.Dispose();
            base.OnClosed(e);
        }

        private void BtnAddReview_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!int.TryParse(txtReviewTripId.Text, out int tripId))
                {
                    MessageBox.Show("❌ Введите номер поездки", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (cbRating.SelectedItem == null)
                {
                    MessageBox.Show("❌ Выберите оценку", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int rating = int.Parse((cbRating.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "5");
                string comment = txtReviewComment.Text.Trim();

                var review = _module.CreateReview(tripId, rating, comment);

                txtReviewResult.Text = "✅ Отзыв успешно оставлен!";
                txtReviewResult.Foreground = new SolidColorBrush(Color.FromRgb(56, 142, 60));

                txtReviewTripId.Clear();
                txtReviewComment.Clear();
                cbRating.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                txtReviewResult.Text = $"❌ Ошибка: {ex.Message}";
                txtReviewResult.Foreground = new SolidColorBrush(Color.FromRgb(211, 47, 47));
            }
        }

        private void BtnShowReviews_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!int.TryParse(txtViewReviewTripId.Text, out int tripId))
                {
                    MessageBox.Show("❌ Введите номер поездки", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var reviews = _module.GetTripReviews(tripId);

                var reviewsWithStars = reviews.Select(r => new
                {
                    r.ReviewID,
                    RatingStars = new string('⭐', r.Rating),
                    r.Comment,
                    UserName = r.Trip.User.FullName,
                    r.CreatedAt
                }).ToList();

                icReviews.ItemsSource = reviewsWithStars;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}