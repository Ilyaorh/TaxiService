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

        public bool IsDriverAvailable(int driverId)
        {
            var driver = _context.Drivers.Find(driverId);
            return driver != null && driver.Status.Equals("Available", StringComparison.OrdinalIgnoreCase);
        }

        public List<Driver> GetAvailableDrivers()
        {
            return _context.Drivers
                .Include(d => d.Car)
                .Where(d => d.Status == "Available")
                .ToList();
        }

        #endregion

        #region Методы работы с поездками

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

        public List<Trip> GetUserTripHistory(int userId)
        {
            return _context.Trips
                .Include(t => t.Driver)
                .ThenInclude(d => d!.Car)
                .Include(t => t.Tariff)
                .Include(t => t.User)
                .Where(t => t.UserID == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ToList();
        }

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

        #region Формирование квитанции

        public string GenerateReceipt(int tripId)
        {
            var trip = _context.Trips
                .Include(t => t.User)
                .Include(t => t.Driver)
                .ThenInclude(d => d!.Car)
                .Include(t => t.Tariff)
                .Include(t => t.Payment)
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
            receipt.AppendLine($"  Стоимость поездки: {trip.TotalCost} ₽");
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

        public List<Tariff> GetAllTariffs()
        {
            return _context.Tariffs.ToList();
        }

        public List<User> GetAllUsers()
        {
            return _context.Users.ToList();
        }

        public List<Driver> GetAllDrivers()
        {
            return _context.Drivers.Include(d => d.Car).ToList();
        }

        public void UpdateDriverStatus(int driverId, string status)
        {
            var driver = _context.Drivers.Find(driverId);
            if (driver != null)
            {
                driver.Status = status;
                _context.SaveChanges();
            }
        }

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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion


        #region Методы работы с отзывами
        public Review CreateReview(int tripId, int rating, string? comment)
        {
            var trip = _context.Trips.Find(tripId);
            if (trip == null)
                throw new ArgumentException("Поездка не найдена", nameof(tripId));

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

        public List<Review> GetTripReviews(int tripId)
        {
            return _context.Reviews
                .Include(r => r.Trip)
                .ThenInclude(t => t.User)
                .Where(r => r.TripID == tripId)
                .ToList();
        }

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