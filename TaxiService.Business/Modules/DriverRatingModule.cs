using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using TaxiService.Data;
using TaxiService.Data.Models;

namespace TaxiService.Business.Modules
{
    /// <summary> Результат операции проверки или добавления отзыва/// </summary>
    public class DriverReviewResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public ReviewStatus Status { get; set; }
    }

    /// <summary> Статус отзыва при проверке возможности его оставления/// </summary>
    public enum ReviewStatus { Added, Unavailable, AlreadyLeft }

    /// <summary> Бизнес-модуль для управления отзывами и рейтингами водителей/// </summary>
    public class DriverRatingModule : IDisposable
    {
        private readonly TaxiServiceContext _context;
        private bool _disposed;

        public DriverRatingModule() { _context = new TaxiServiceContext(); }
        public DriverRatingModule(TaxiServiceContext context) { _context = context; }

        /// <summary> Проверяет возможность оставления отзыва пользователем для указанной поездки/// </summary>
        /// <returns>Объект результата проверки</returns>
        public DriverReviewResult CanLeaveReview(int tripId, int userId)
        {
            var trip = _context.Trips
                .Include(t => t.Driver)
                .FirstOrDefault(t => t.TripID == tripId);

            if (trip == null) return new DriverReviewResult { Success = false, Message = "Поездка не найдена", Status = ReviewStatus.Unavailable };
            if (trip.UserID != userId) return new DriverReviewResult { Success = false, Message = "Вы не можете оставить отзыв на эту поездку", Status = ReviewStatus.Unavailable };
            if (trip.Status != "Completed") return new DriverReviewResult { Success = false, Message = "Отзыв можно оставить только после завершения поездки", Status = ReviewStatus.Unavailable };

            var existingReview = _context.Reviews.FirstOrDefault(r => r.TripID == tripId);
            if (existingReview != null) return new DriverReviewResult { Success = false, Message = "Вы уже оставили отзыв на эту поездку", Status = ReviewStatus.AlreadyLeft };

            return new DriverReviewResult { Success = true, Message = "Можно оставить отзыв", Status = ReviewStatus.Added };
        }

        /// <summary> Добавляет отзыв о водителе и обновляет его рейтинг в базе данных/// </summary>
        /// <returns>Результат операции добавления</returns>
        public DriverReviewResult AddDriverReview(int tripId, int userId, int rating, string comment)
        {
            var canLeaveResult = CanLeaveReview(tripId, userId);
            if (!canLeaveResult.Success) return canLeaveResult;

            var trip = _context.Trips.Find(tripId);
            if (trip == null || !trip.DriverID.HasValue)
                return new DriverReviewResult { Success = false, Message = "Поездка не найдена или у неё нет водителя", Status = ReviewStatus.Unavailable };

            var review = new Review { TripID = tripId, Rating = rating, Comment = comment, CreatedAt = DateTime.Now };
            _context.Reviews.Add(review);
            _context.SaveChanges();

            UpdateDriverRatingInternal(trip.DriverID.Value);
            return new DriverReviewResult { Success = true, Message = "Отзыв успешно добавлен", Status = ReviewStatus.Added };
        }

        /// <summary> Пересчитывает средний рейтинг водителя на основе всех его завершённых поездок/// </summary>
        private void UpdateDriverRatingInternal(int driverId)
        {
            var reviews = _context.Reviews
                .Where(r => r.Trip.DriverID == driverId && r.Trip.Status == "Completed")
                .ToList();

            var avgRating = reviews.Any() ? Math.Round(reviews.Average(r => (decimal)r.Rating), 2) : 0.00m;

            var driverRating = _context.DriverRatings.FirstOrDefault(dr => dr.DriverID == driverId);
            if (driverRating == null)
            {
                driverRating = new DriverRating { DriverID = driverId, AverageRating = avgRating, ReviewCount = reviews.Count, LastUpdatedAt = DateTime.Now };
                _context.DriverRatings.Add(driverRating);
            }
            else
            {
                driverRating.AverageRating = avgRating;
                driverRating.ReviewCount = reviews.Count;
                driverRating.LastUpdatedAt = DateTime.Now;
            }

            var driver = _context.Drivers.Find(driverId);
            if (driver != null) driver.Rating = avgRating;

            _context.SaveChanges();
        }

        /// <summary> Получает расширенную информацию о рейтинге конкретного водителя/// </summary>
        /// <returns>Объект рейтинга водителя или null, если рейтинг не найден</returns>
        public DriverRating? GetDriverRating(int driverId)
        {
            return _context.DriverRatings
                .Include(dr => dr.Driver)
                .ThenInclude(d => d.Car)
                .FirstOrDefault(dr => dr.DriverID == driverId);
        }

        /// <summary> Возвращает список рейтингов всех водителей, отсортированный по убыванию рейтинга и количеству отзывов/// </summary>
        /// <returns>Список объектов рейтингов водителей</returns>
        public List<DriverRating> GetAllDriverRatingsSorted()
        {
            return _context.DriverRatings
                .Include(dr => dr.Driver)
                .ThenInclude(d => d.Car)
                .OrderByDescending(dr => dr.AverageRating)
                .ThenByDescending(dr => dr.ReviewCount)
                .ToList();
        }

        /// <summary> Возвращает все отзывы о водителе, отсортированные по дате создания/// </summary>
        /// <returns>Список отзывов</returns>
        public List<Review> GetDriverReviews(int driverId)
        {
            return _context.Reviews
                .Where(r => r.Trip.DriverID == driverId)
                .Include(r => r.Trip)
                .ThenInclude(t => t.User)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();
        }

        /// <summary> Рассчитывает среднюю оценку водителя на основе завершённых поездок/// </summary>
        /// <returns>Средний рейтинг (округлённый до 2 знаков)</returns>
        public decimal CalculateAverageRating(int driverId)
        {
            var reviews = _context.Reviews
                .Where(r => r.Trip.DriverID == driverId && r.Trip.Status == "Completed")
                .ToList();
            return reviews.Any() ? Math.Round(reviews.Average(r => (decimal)r.Rating), 2) : 0.00m;
        }

        /// <summary> Возвращает текстовое описание статуса возможности оставить отзыв/// </summary>
        /// <returns>Текстовое описание статуса</returns>
        public string GetReviewStatusText(int tripId, int userId)
        {
            var result = CanLeaveReview(tripId, userId);
            return result.Status switch { ReviewStatus.Added => "Добавлен", ReviewStatus.Unavailable => "Недоступен", ReviewStatus.AlreadyLeft => "Уже оставлен", _ => "Неизвестно" };
        }

        protected virtual void Dispose(bool disposing) { if (!_disposed) { if (disposing) _context?.Dispose(); _disposed = true; } }
        public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }
    }
}