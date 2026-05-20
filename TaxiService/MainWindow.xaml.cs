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
        private readonly DriverRatingModule _ratingModule;
        private decimal _currentOriginalCost = 0;
        private decimal _currentDiscount = 0;
        private PromoCode? _appliedPromoCode = null;
        private bool _sortDescending = true; 

        public MainWindow()
        {
            InitializeComponent();
            _module = new TaxiRideModule();
            _ratingModule = new DriverRatingModule();
            LoadInitialData();
            LoadDriverRatings();
        }


        /// <summary>Загружает начальные данные</summary>
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
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>Обновляет список водителей в UI</summary>
        private void LoadDrivers()
        {
            var drivers = _module.GetAllDrivers();
            cbDrivers.ItemsSource = drivers;
            cbDrivers.DisplayMemberPath = "FullName";
            cbDrivers.SelectedValuePath = "DriverID";

            var availableDrivers = _module.GetAvailableDrivers();
            if (_sortDescending)
            {
                icDrivers.ItemsSource = availableDrivers
                .OrderByDescending(d => d.Rating)
                .ThenBy(d => d.FullName)
                .ToList();
            }
            else
            {
                icDrivers.ItemsSource = availableDrivers
                .OrderBy(d => d.Rating)
                .ThenBy(d => d.FullName)
                .ToList();
            }
        }

        /// <summary>Загружает и отображает отсортированные рейтинги водителей</summary>
        private void LoadDriverRatings()
        {
            try
            {
                var ratings = _ratingModule.GetAllDriverRatingsSorted();
                icDriverRatings.ItemsSource = ratings;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки рейтингов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>Пересчитывает стоимость поездки при смене тарифа</summary>
        private void CbTariffs_SelectionChanged(object sender, SelectionChangedEventArgs e) => CalculateCost();

        /// <summary>Пересчитывает стоимость поездки при изменении расстояния</summary>
        private void TxtDistance_TextChanged(object sender, TextChangedEventArgs e) => CalculateCost();

        /// <summary>Рассчитывает стоимость поездки, комиссию и обновляет UI</summary>
        private void CalculateCost()
        {
            if (cbTariffs.SelectedItem is Tariff selectedTariff && decimal.TryParse(txtDistance.Text, out decimal distance))
            {
                try
                {
                    var totalCost = _module.CalculateTripCost(distance, selectedTariff.TariffID);
                    var commission = _module.CalculateServiceCommission(totalCost, selectedTariff.TariffID);
                    _currentOriginalCost = totalCost;

                    txtTariffInfo.Text = selectedTariff.Name;
                    txtBasePrice.Text = $"📍 Базовая стоимость: {selectedTariff.BasePrice} ₽";
                    txtPricePerKm.Text = $"📏 Цена за км: {selectedTariff.PricePerKm} ₽/км";
                    txtCalculationDetails.Text = $"Формула расчёта:\n{selectedTariff.BasePrice} + ({distance} км × {selectedTariff.PricePerKm} ₽) = {totalCost} ₽\nКомиссия сервиса ({selectedTariff.CommissionPercent}%): {commission} ₽";

                    txtTotalCost.Text = _currentDiscount > 0 ? $"{_currentOriginalCost - _currentDiscount} ₽" : $"{totalCost} ₽";
                    txtCommission.Text = $"В т.ч. комиссия сервиса: {commission} ₽";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка расчёта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        /// <summary>Применяет промокод к текущей поездке с валидацией</summary>
        private void BtnApplyPromoCode_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cbUsers.SelectedItem == null) { MessageBox.Show("❌ Сначала выберите пассажира", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
                string promoCodeText = txtPromoCode.Text.Trim();
                if (string.IsNullOrEmpty(promoCodeText)) { MessageBox.Show("❌ Введите промокод", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
                if (!(cbTariffs.SelectedItem is Tariff selectedTariff && decimal.TryParse(txtDistance.Text, out decimal distance))) { MessageBox.Show("❌ Сначала заполните данные о поездке", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

                _currentOriginalCost = _module.CalculateTripCost(distance, selectedTariff.TariffID);
                int userId = (int)cbUsers.SelectedValue;
                var result = _module.ValidateAndApplyPromoCode(promoCodeText, userId, _currentOriginalCost);

                if (result.Success)
                {
                    txtPromoStatus.Text = $"✅ Статус: {result.Message}";
                    txtPromoStatus.Foreground = new SolidColorBrush(Color.FromRgb(56, 142, 60));
                    txtOriginalCost.Text = $"Исходная стоимость: {_currentOriginalCost:F2} ₽";
                    txtDiscountAmount.Text = $"Скидка: -{result.DiscountAmount:F2} ₽";
                    txtFinalCost.Text = $"Итого: {result.FinalCost:F2} ₽";
                    txtTotalCost.Text = $"{result.FinalCost:F2} ₽";
                    _currentDiscount = result.DiscountAmount;
                    _appliedPromoCode = result.PromoCode;
                    MessageBox.Show($"✅ Промокод применён!\nСкидка: {result.DiscountAmount:F2} ₽", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    txtPromoStatus.Text = $"❌ Статус: {result.Message}";
                    txtPromoStatus.Foreground = new SolidColorBrush(Color.FromRgb(211, 47, 47));
                    txtOriginalCost.Text = $"Исходная стоимость: {_currentOriginalCost:F2} ₽";
                    txtDiscountAmount.Text = "Скидка: 0 ₽";
                    txtFinalCost.Text = $"Итого: {_currentOriginalCost:F2} ₽";
                    txtTotalCost.Text = $"{_currentOriginalCost:F2} ₽";
                    _currentDiscount = 0;
                    _appliedPromoCode = null;
                    MessageBox.Show($"❌ {result.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex) { MessageBox.Show($"❌ Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
        }


        /// <summary>Создаёт новую поездку с применением промокода (если есть)</summary>
        private void BtnCreateTrip_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cbUsers.SelectedItem == null) { MessageBox.Show("❌ Выберите пассажира", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
                if (cbTariffs.SelectedItem == null) { MessageBox.Show("❌ Выберите тариф", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
                if (string.IsNullOrWhiteSpace(txtStartAddress.Text) || string.IsNullOrWhiteSpace(txtEndAddress.Text)) { MessageBox.Show("❌ Введите адреса", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
                if (!decimal.TryParse(txtDistance.Text, out decimal distance) || distance <= 0) { MessageBox.Show("❌ Введите корректное расстояние", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

                int userId = (int)cbUsers.SelectedValue;
                int tariffId = (int)cbTariffs.SelectedValue;
                int? driverId = cbDrivers.SelectedValue as int?;

                var trip = _module.CreateTrip(userId, driverId, tariffId, txtStartAddress.Text, txtEndAddress.Text, distance);

                if (_appliedPromoCode != null && _currentDiscount > 0)
                    _module.ApplyPromoCodeToTrip(trip.TripID, _appliedPromoCode.PromoCodeID, userId, _currentDiscount);

                MessageBox.Show($"✅ Поездка №{trip.TripID} создана!\nСтоимость: {trip.TotalCost} ₽", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                txtStartAddress.Clear(); txtEndAddress.Clear(); txtDistance.Clear(); txtPromoCode.Clear();
                txtPromoStatus.Text = ""; txtOriginalCost.Text = ""; txtDiscountAmount.Text = ""; txtFinalCost.Text = "";
                cbDrivers.SelectedIndex = -1; _currentDiscount = 0; _appliedPromoCode = null;
                LoadDrivers();
            }
            catch (Exception ex) { MessageBox.Show($"❌ Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
        }


        /// <summary>Обновляет список доступных водителей</summary>
        private void BtnRefreshDrivers_Click(object sender, RoutedEventArgs e) => LoadDrivers();


        /// <summary>Открывает диалог изменения статуса выбранного водителя</summary>
        private void BtnChangeDriverStatus_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int driverId)
            {
                var dialog = new StatusChangeDialog(new[] { "Available", "Busy", "Offline" });
                dialog.Owner = this;
                if (dialog.ShowDialog() == true) { _module.UpdateDriverStatus(driverId, dialog.SelectedStatus); LoadDrivers(); }
            }
        }


        /// <summary>Обработчик изменения выбранного пользователя в истории поездок</summary>
        private void CbTripUsers_SelectionChanged(object sender, SelectionChangedEventArgs e) { }


        /// <summary>Отображает историю поездок выбранного пользователя в DataGrid</summary>
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


        /// <summary>Обработчик выбора поездки в списке истории</summary>
        private void DgTrips_SelectionChanged(object sender, SelectionChangedEventArgs e) { }


        /// <summary>Обрабатывает оплату поездки и генерирует квитанцию</summary>
        private void BtnPay_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!int.TryParse(txtPaymentTripId.Text, out int tripId)) { MessageBox.Show("❌ Введите номер поездки", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
                if (!decimal.TryParse(txtPaymentAmount.Text, out decimal amount) || amount <= 0) { MessageBox.Show("❌ Введите сумму", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

                string method = (cbPaymentMethod.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Cash";
                var payment = _module.CreatePayment(tripId, amount, method);
                _module.CompleteTrip(tripId);
                string receipt = _module.GenerateReceipt(tripId);

                txtPaymentResult.Text = $"✅ Оплата прошла успешно!\nПлатёж №{payment.PaymentID}";
                MessageBox.Show(receipt, "📄 Квитанция", MessageBoxButton.OK, MessageBoxImage.Information);
                txtPaymentTripId.Clear(); txtPaymentAmount.Clear();
            }
            catch (Exception ex) { MessageBox.Show($"❌ Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
        }


        /// <summary>Добавляет отзыв водителю с валидацией прав и статуса поездки</summary>
        private void BtnAddDriverReview_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!int.TryParse(txtReviewTripIdNew.Text, out int tripId)) { MessageBox.Show("❌ Введите корректный номер поездки", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
                if (!int.TryParse(txtReviewUserId.Text, out int userId)) { MessageBox.Show("❌ Введите корректный ID пользователя", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
                if (cbReviewRating.SelectedItem == null) { MessageBox.Show("❌ Выберите оценку", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

                int rating = int.Parse((cbReviewRating.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "5");
                string comment = txtReviewCommentNew.Text.Trim();

                var result = _ratingModule.AddDriverReview(tripId, userId, rating, comment);
                txtReviewStatusMessage.Text = result.Message;
                txtReviewStatusMessage.Foreground = result.Success ? new SolidColorBrush(Color.FromRgb(56, 142, 60)) : new SolidColorBrush(Color.FromRgb(211, 47, 47));

                if (result.Success)
                {
                    MessageBox.Show($"✅ {result.Message}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    txtReviewTripIdNew.Clear(); txtReviewCommentNew.Clear(); cbReviewRating.SelectedIndex = -1;
                    LoadDriverRatings();
                }
                else
                {
                    string statusMessage = result.Status switch { ReviewStatus.Unavailable => "❌ Отзыв недоступен для этой поездки", ReviewStatus.AlreadyLeft => "⚠️ Вы уже оставили отзыв на эту поездку", _ => "❌ Ошибка при добавлении отзыва" };
                    MessageBox.Show(statusMessage, "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex) { MessageBox.Show($" Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
        }


        /// <summary>Отображает отзывы выбранного водителя в ItemsControl</summary>
        private void BtnShowDriverReviews_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is int driverId)
                {
                    var reviews = _ratingModule.GetDriverReviews(driverId);
                    var reviewsWithInfo = reviews.Select(r => new
                    {
                        r.ReviewID,
                        RatingStars = new string('⭐', r.Rating),
                        r.Comment,
                        UserName = r.Trip?.User?.FullName ?? "Неизвестно",
                        r.CreatedAt
                    }).ToList();
                    icSelectedDriverReviews.ItemsSource = reviewsWithInfo;
                }
            }
            catch (Exception ex) { MessageBox.Show($"Ошибка загрузки отзывов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
        }


        /// <summary>Освобождает ресурсы модулей при закрытии окна</summary>
        protected override void OnClosed(EventArgs e)
        {
            _module?.Dispose();
            _ratingModule?.Dispose();
            base.OnClosed(e);
        }

        /// <summary>Переключает направление сортировки водителей</summary>
        private void BtnSortDrivers_Click(object sender, RoutedEventArgs e)
        {
            _sortDescending = !_sortDescending;
            btnSortDrivers.Content = _sortDescending
            ? "📊 Сортировка: по убыванию ⬇"
            : "📊 Сортировка: по возрастанию ⬆";
            LoadDrivers();
        }
    }
}