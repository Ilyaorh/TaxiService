using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaxiService.Data;
using TaxiService.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace TaxiService.Business.Modules
{
    public class TaxiRideModule : IDisposable
    {
        private readonly TaxiServiceContext _context;
        private bool _disposed;

        public TaxiRideModule()
        {
            _context = new TaxiServiceContext();
        }

        public TaxiRideModule(TaxiServiceContext context)
        {
            _context = context;
        }

        #region Методы расчёта стоимости

        /// <summary>Рассчитывает стоимость поездки по расстоянию и тарифу</summary>
        /// <returns>Округлённая стоимость поездки</returns>
        public decimal CalculateTripCost(decimal distanceKm, int tariffId)
        {
            if (distanceKm < 0)
                throw new ArgumentException("Расстояние не может быть отрицательным", nameof(distanceKm));

            var tariff = _context.Tariffs.Find(tariffId);
            if (tariff == null)
                throw new ArgumentException("Тариф не найден", nameof(tariffId));

            decimal cost = tariff.BasePrice + (distanceKm * tariff.PricePerKm);
            return Math.Round(cost, 2);
        }

        /// <summary>Вычисляет комиссию сервиса от общей стоимости поездки</summary>
        /// <returns>Сумма комиссии</returns>
        public decimal CalculateServiceCommission(decimal totalCost, int tariffId)
        {
            var tariff = _context.Tariffs.Find(tariffId);
            if (tariff == null)
                throw new ArgumentException("Тариф не найден", nameof(tariffId));

            decimal commission = totalCost * (tariff.CommissionPercent / 100);
            return Math.Round(commission, 2);
        }
        #endregion

        #region Методы проверки доступности

        /// <summary>Проверяет, свободен ли водитель для новой поездки</summary>
        /// <returns>True, если статус водителя "Available"</returns>
        public bool IsDriverAvailable(int driverId)
        {
            var driver = _context.Drivers.Find(driverId);
            return driver != null && driver.Status.Equals("Available", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Возвращает список всех свободных водителей с их автомобилями</summary>
        /// <returns>Список доступных водителей</returns>
        public List<Driver> GetAvailableDrivers()
        {
            return _context.Drivers
                .Include(d => d.Car)
                .Where(d => d.Status == "Available")
                .ToList();
        }
        #endregion

        #region Методы работы с поездками

        /// <summary>Создаёт новую поездку, рассчитывает стоимость и меняет статус водителя на "Busy"</summary>
        /// <returns>Созданный объект поездки</returns>
        public Trip CreateTrip(int userId, int? driverId, int tariffId,
            string startAddress, string endAddress, decimal distanceKm)
        {
            var totalCost = CalculateTripCost(distanceKm, tariffId);
            var commission = CalculateServiceCommission(totalCost, tariffId);

            var trip = new Trip
            {
                UserID = userId,
                DriverID = driverId,
                TariffID = tariffId,
                StartAddress = startAddress,
                EndAddress = endAddress,
                DistanceKm = distanceKm,
                TotalCost = totalCost,
                ServiceCommission = commission,
                Status = "Created",
                CreatedAt = DateTime.Now
            };

            _context.Trips.Add(trip);
            _context.SaveChanges();

            if (driverId.HasValue)
            {
                var driver = _context.Drivers.Find(driverId.Value);
                if (driver != null)
                {
                    driver.Status = "Busy";
                    _context.SaveChanges();
                }
            }

            return trip;
        }

        /// <summary>Возвращает историю поездок пользователя, отсортированную по дате</summary>
        /// <returns>Список поездок</returns>
        public List<Trip> GetUserTripHistory(int userId)
        {
            return _context.Trips
                .Include(t => t.Driver)
                .ThenInclude(d => d!.Car)
                .Include(t => t.Tariff)
                .Include(t => t.User)
                .Include(t => t.PromoCode)
                .Where(t => t.UserID == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ToList();
        }

        /// <summary>Завершает поездку и возвращает водителю статус "Available"</summary>
        public void CompleteTrip(int tripId)
        {
            var trip = _context.Trips.Find(tripId);
            if (trip != null)
            {
                trip.Status = "Completed";
                trip.CompletedAt = DateTime.Now;

                if (trip.DriverID.HasValue)
                {
                    var driver = _context.Drivers.Find(trip.DriverID.Value);
                    if (driver != null)
                    {
                        driver.Status = "Available";
                    }
                }

                _context.SaveChanges();
            }
        }
        #endregion

        #region Промокоды и скидки
        public class PromoCodeResult
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public decimal DiscountAmount { get; set; }
            public decimal FinalCost { get; set; }
            public PromoCode? PromoCode { get; set; }
        }


        /// <summary>Проверяет валидность промокода, рассчитывает скидку и возвращает результат применения</summary>
        public PromoCodeResult ValidateAndApplyPromoCode(string code, int userId, decimal originalCost)
        {
            var result = new PromoCodeResult();

            if (string.IsNullOrWhiteSpace(code))
            {
                result.Success = false;
                result.Message = "Введите код промокода";
                return result;
            }

            string normalizedCode = code.Trim().ToLower();
            var promoCode = _context.PromoCodes
                .FirstOrDefault(p => p.Code.ToLower() == normalizedCode);

            if (promoCode == null)
            {
                result.Success = false;
                result.Message = "Промокод не найден";
                return result;
            }

            if (!promoCode.IsActive)
            {
                result.Success = false;
                result.Message = "Недействителен";
                return result;
            }

            var now = DateTime.Now;
            if (now < promoCode.StartDate)
            {
                result.Success = false;
                result.Message = $"Промокод ещё не активен. Начало: {promoCode.StartDate:dd.MM.yyyy}";
                return result;
            }

            if (now > promoCode.EndDate)
            {
                result.Success = false;
                result.Message = "Истёк срок действия";
                return result;
            }

            decimal discountAmount = promoCode.DiscountType == "Percent"
                ? originalCost * (promoCode.DiscountValue / 100)
                : promoCode.DiscountValue;

            if (discountAmount > originalCost)
                discountAmount = originalCost;

            decimal finalCost = originalCost - discountAmount;

            result.Success = true;
            result.Message = "Применён";
            result.DiscountAmount = Math.Round(discountAmount, 2);
            result.FinalCost = Math.Round(finalCost, 2);
            result.PromoCode = promoCode;

            return result;
        }


        /// <summary> Сохраняет факт использования промокода в БД и обновляет итоговую стоимость поездки/// </summary>
        public PromoCodeUsage ApplyPromoCodeToTrip(int tripId, int promoCodeId, int userId, decimal discountAmount)
        {
            var usage = new PromoCodeUsage
            {
                TripID = tripId,
                PromoCodeID = promoCodeId,
                UserID = userId,
                DiscountAmount = discountAmount,
                AppliedAt = DateTime.Now
            };

            _context.PromoCodeUsages.Add(usage);

            var trip = _context.Trips.Find(tripId);
            if (trip != null)
            {
                trip.PromoCodeID = promoCodeId;
                trip.DiscountAmount = discountAmount;
                trip.OriginalCost = trip.TotalCost;
                trip.TotalCost = trip.TotalCost - discountAmount;
            }

            _context.SaveChanges();
            return usage;
        }


        /// <summary>Возвращает список всех активных и действующих на текущую дату промокодов/// </summary>
        public List<PromoCode> GetAllActivePromoCodes()
        {
            var now = DateTime.Now;
            return _context.PromoCodes
                .Where(p => p.IsActive && p.StartDate <= now && p.EndDate >= now)
                .ToList();
        }

        /// <summary> Ищет промокод по его строковому коду/// </summary>
        public PromoCode? GetPromoCodeByCode(string code)
        {
            string normalizedCode = code.Trim().ToLower();
            return _context.PromoCodes
                .FirstOrDefault(p => p.Code.ToLower() == normalizedCode);
        }
        #endregion

        #region Формирование квитанции

        /// <summary>Формирует текстовую квитанцию с деталями поездки, оплаты и маршрута</summary>
        /// <returns>Строка с форматированной квитанцией</returns>
        public string GenerateReceipt(int tripId)
        {
            var trip = _context.Trips
                .Include(t => t.User)
                .Include(t => t.Driver)
                .ThenInclude(d => d!.Car)
                .Include(t => t.Tariff)
                .Include(t => t.Payment)
                .Include(t => t.PromoCode)
                .FirstOrDefault(t => t.TripID == tripId);

            if (trip == null)
                throw new ArgumentException("Поездка не найдена", nameof(tripId));

            var receipt = new StringBuilder();
            receipt.AppendLine("═══════════════════════════════════════");
            receipt.AppendLine("           КВИТАНЦИЯ ПОЕЗДКИ");
            receipt.AppendLine("═══════════════════════════════════════");
            receipt.AppendLine($"№ поездки: {trip.TripID}");
            receipt.AppendLine($"Дата: {trip.CreatedAt:dd.MM.yyyy HH:mm}");
            receipt.AppendLine($"Статус: {trip.Status}");
            receipt.AppendLine("───────────────────────────────────────");
            receipt.AppendLine("ПАССАЖИР:");
            receipt.AppendLine($"  {trip.User.FullName}");
            receipt.AppendLine($"  {trip.User.Phone}");
            receipt.AppendLine("───────────────────────────────────────");

            if (trip.Driver != null)
            {
                receipt.AppendLine("ВОДИТЕЛЬ:");
                receipt.AppendLine($"  {trip.Driver.FullName}");
                receipt.AppendLine($"  {trip.Driver.Phone}");
                if (trip.Driver.Car != null)
                {
                    receipt.AppendLine($"  Авто: {trip.Driver.Car.Model} {trip.Driver.Car.PlateNumber}");
                }
            }

            receipt.AppendLine("───────────────────────────────────────");
            receipt.AppendLine("МАРШРУТ:");
            receipt.AppendLine($"  Откуда: {trip.StartAddress}");
            receipt.AppendLine($"  Куда: {trip.EndAddress}");
            receipt.AppendLine($"  Расстояние: {trip.DistanceKm} км");
            receipt.AppendLine("───────────────────────────────────────");
            receipt.AppendLine("ТАРИФ:");
            receipt.AppendLine($"  {trip.Tariff.Name}");
            receipt.AppendLine($"  Посадка: {trip.Tariff.BasePrice} ₽");
            receipt.AppendLine($"  За км: {trip.Tariff.PricePerKm} ₽/км");
            receipt.AppendLine("───────────────────────────────────────");
            receipt.AppendLine("СТОИМОСТЬ:");

            if (trip.DiscountAmount.HasValue && trip.DiscountAmount > 0)
            {
                receipt.AppendLine($"  Исходная стоимость: {trip.OriginalCost} ₽");
                receipt.AppendLine($"  Скидка: -{trip.DiscountAmount} ₽");
                if (trip.PromoCode != null)
                {
                    receipt.AppendLine($"  Промокод: {trip.PromoCode.Code}");
                }
                receipt.AppendLine($"  ИТОГО СО СКИДКОЙ: {trip.TotalCost} ₽");
            }
            else
            {
                receipt.AppendLine($"  Стоимость поездки: {trip.TotalCost} ₽");
            }

            receipt.AppendLine($"  Комиссия сервиса: {trip.ServiceCommission} ₽");
            receipt.AppendLine($"  ИТОГО: {trip.TotalCost} ₽");
            receipt.AppendLine("───────────────────────────────────────");

            if (trip.Payment != null)
            {
                receipt.AppendLine("ОПЛАТА:");
                receipt.AppendLine($"  Способ: {(trip.Payment.Method == "Card" ? "Карта" : "Наличные")}");
                receipt.AppendLine($"  Дата: {trip.Payment.PaymentDate:dd.MM.yyyy HH:mm}");
            }

            receipt.AppendLine("═══════════════════════════════════════");
            receipt.AppendLine("   Спасибо за пользование сервисом!");
            receipt.AppendLine("═══════════════════════════════════════");

            return receipt.ToString();
        }
        #endregion

        #region Получение данных

        /// <summary>Возвращает все тарифы из базы данных</summary>
        /// <returns>Список тарифов</returns>
        public List<Tariff> GetAllTariffs()
        {
            return _context.Tariffs.ToList();
        }


        /// <summary>Возвращает всех зарегистрированных пользователей</summary>
        /// <returns>Список пользователей</returns>
        public List<User> GetAllUsers()
        {
            return _context.Users.ToList();
        }



        /// <summary>Возвращает всех водителей с привязанными автомобилями</summary>
        /// <returns>Список водителей</returns>
        public List<Driver> GetAllDrivers()
        {
            return _context.Drivers.Include(d => d.Car).ToList();
        }

        /// <summary>Обновляет статус конкретного водителя в базе данных</summary>
        public void UpdateDriverStatus(int driverId, string status)
        {
            var driver = _context.Drivers.Find(driverId);
            if (driver != null)
            {
                driver.Status = status;
                _context.SaveChanges();
            }
        }

        /// <summary>Создаёт запись об оплате для указанной поездки</summary>
        /// <returns>Созданный объект платежа</returns>
        public Payment CreatePayment(int tripId, decimal amount, string method)
        {
            var payment = new Payment
            {
                TripID = tripId,
                Amount = amount,
                PaymentDate = DateTime.Now,
                Method = method
            };

            _context.Payments.Add(payment);
            _context.SaveChanges();
            return payment;
        }
        #endregion

        #region IDisposable

        /// <summary>Освобождает управляемые ресурсы, включая контекст БД</summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _context?.Dispose();
                }
                _disposed = true;
            }
        }


        /// <summary>Освобождает все ресурсы, используемые модулем</summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        #region Методы работы с отзывами


        /// <summary>Создаёт отзыв к поездке и пересчитывает средний рейтинг водителя</summary>
        /// <returns>Созданный объект отзыва</returns>
        public Review CreateReview(int tripId, int rating, string? comment)
        {
            var trip = _context.Trips.Find(tripId);
            if (trip == null)
                throw new ArgumentException("Поездка не найдена", nameof(tripId));

            if (trip.Status != "Completed")
                throw new InvalidOperationException("Отзыв можно оставить только после завершения поездки");

            var existingReview = _context.Reviews.FirstOrDefault(r => r.TripID == tripId);
            if (existingReview != null)
                throw new InvalidOperationException("Отзыв на эту поездку уже оставлен");

            var review = new Review
            {
                TripID = tripId,
                Rating = rating,
                Comment = comment,
                CreatedAt = DateTime.Now
            };
            _context.Reviews.Add(review);

            if (trip.DriverID.HasValue)
            {
                var driver = _context.Drivers.Find(trip.DriverID.Value);
                if (driver != null)
                {
                    var avgRating = _context.Reviews
                        .Where(r => r.Trip.DriverID == driver.DriverID)
                        .Average(r => (decimal?)r.Rating) ?? 5.00m;
                    driver.Rating = Math.Round(avgRating, 2);
                }
            }
            _context.SaveChanges();
            return review;
        }


        /// <summary>Возвращает все отзывы, оставленные к конкретной поездке</summary>
        /// <returns>Список отзывов</returns>
        public List<Review> GetTripReviews(int tripId)
        {
            return _context.Reviews
                .Include(r => r.Trip)
                .ThenInclude(t => t.User)
                .Where(r => r.TripID == tripId)
                .ToList();
        }


        /// <summary>Возвращает все отзывы о водителе, отсортированные по дате</summary>
        /// <returns>Список отзывов</returns>
        public List<Review> GetDriverReviews(int driverId)
        {
            return _context.Reviews
                .Include(r => r.Trip)
                .ThenInclude(t => t.User)
                .Where(r => r.Trip.DriverID == driverId)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();
        }
        #endregion
    }
}