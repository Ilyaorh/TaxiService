using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TaxiService.Business.Modules; 
using TaxiService.Data;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace TaxiService.Tests
{
    [TestClass]
    public class TaxiRideModuleTests
    {
        private TaxiRideModule _module = null!;
        private DriverRatingModule _ratingModule = null!; 

        [TestInitialize]
        public void Setup()
        {
            _module = new TaxiRideModule();
            _ratingModule = new DriverRatingModule(); 
        }

        [TestCleanup]
        public void Cleanup()
        {
            _module?.Dispose();
            _ratingModule?.Dispose(); 
        }

        #region Тесты расчёта стоимости

        [TestMethod]
        public void CalculateTripCost_ValidInput_ReturnsCorrectCost() // Проверяет корректный расчёт стоимости
        {
            decimal distance = 10;
            int tariffId = 1;
            decimal result = _module.CalculateTripCost(distance, tariffId);
            Assert.AreEqual(200.00m, result);
        }

        [TestMethod]
        public void CalculateTripCost_ZeroDistance_ReturnsBasePrice() // Проверяет возврат базовой цены при нулевом расстоянии
        {
            decimal distance = 0;
            int tariffId = 1;
            decimal result = _module.CalculateTripCost(distance, tariffId);
            Assert.AreEqual(50.00m, result);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void CalculateTripCost_NegativeDistance_ThrowsException() // Проверяет выброс исключения при отрицательном расстоянии
        {
            _module.CalculateTripCost(-5, 1);
        }

        [TestMethod]
        public void CalculateServiceCommission_ValidInput_ReturnsCorrectCommission() // Проверяет корректный расчёт комиссии сервиса
        {
            decimal totalCost = 200;
            int tariffId = 1;
            decimal result = _module.CalculateServiceCommission(totalCost, tariffId);
            Assert.AreEqual(40.00m, result);
        }

        #endregion

        #region Тесты доступности водителей

        [TestMethod]
        public void IsDriverAvailable_AvailableDriver_ReturnsTrue() // Проверяет статус Available возвращает True
        {
            int driverId = 1;
            bool result = _module.IsDriverAvailable(driverId);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsDriverAvailable_BusyDriver_ReturnsFalse() // Проверяет статус Busy возвращает False
        {
            int driverId = 1;
            _module.UpdateDriverStatus(driverId, "Busy");
            bool result = _module.IsDriverAvailable(driverId);
            Assert.IsFalse(result, "Водитель со статусом Busy должен быть недоступен");
            _module.UpdateDriverStatus(driverId, "Available");
        }

        #endregion

        #region Тесты квитанций

        [TestMethod]
        public void GenerateReceipt_ValidTripId_ReturnsReceiptString() // Проверяет генерацию чека для существующей поездки
        {
            int tripId = 1;
            string receipt = _module.GenerateReceipt(tripId);
            Assert.IsNotNull(receipt);
            Assert.IsTrue(receipt.Contains("КВИТАНЦИЯ ПОЕЗДКИ"));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void GenerateReceipt_InvalidTripId_ThrowsException() // Проверяет выброс исключения для несуществующей поездки
        {
            _module.GenerateReceipt(9999);
        }

        #endregion

        #region Тесты промокодов

        [TestMethod]
        public void ValidateAndApplyPromoCode_ValidPercentPromoCode_ReturnsSuccess() // Проверяет применение фиксированной скидки
        {
            string code = "WELCOME10";
            int userId = 1;
            decimal originalCost = 200;
            var result = _module.ValidateAndApplyPromoCode(code, userId, originalCost);
            Assert.IsTrue(result.Success);
            Assert.AreEqual("Применён", result.Message);
            Assert.AreEqual(20.00m, result.DiscountAmount);
            Assert.AreEqual(180.00m, result.FinalCost);
        }

        [TestMethod]
        public void ValidateAndApplyPromoCode_ValidFixedPromoCode_ReturnsSuccess() // Проверяет применение скидки
        {
            string code = "FIXED50";
            int userId = 1;
            decimal originalCost = 200;
            var result = _module.ValidateAndApplyPromoCode(code, userId, originalCost);
            Assert.IsTrue(result.Success);
            Assert.AreEqual("Применён", result.Message);
            Assert.AreEqual(50.00m, result.DiscountAmount);
            Assert.AreEqual(150.00m, result.FinalCost);
        }

        [TestMethod]
        public void ValidateAndApplyPromoCode_NonExistentCode_ReturnsError() // Проверяет ошибку при несуществующем промокоде
        {
            string code = "INVALID123";
            int userId = 1;
            decimal originalCost = 200;
            var result = _module.ValidateAndApplyPromoCode(code, userId, originalCost);
            Assert.IsFalse(result.Success);
            Assert.AreEqual("Промокод не найден", result.Message);
            Assert.AreEqual(0, result.DiscountAmount);
        }

        [TestMethod]
        public void ValidateAndApplyPromoCode_ExpiredPromoCode_ReturnsError() // Проверяет ошибку при просроченном промокоде    
        {
            string code = "EXPIRED";
            int userId = 1;
            decimal originalCost = 200;
            var result = _module.ValidateAndApplyPromoCode(code, userId, originalCost);
            Assert.IsFalse(result.Success);
            Assert.AreEqual("Истёк срок действия", result.Message);
        }

        [TestMethod]
        public void ValidateAndApplyPromoCode_InactivePromoCode_ReturnsError() // Проверяет ошибку при недействительном промокоде
        {
            string code = "INACTIVE";
            int userId = 1;
            decimal originalCost = 200;
            var result = _module.ValidateAndApplyPromoCode(code, userId, originalCost);
            Assert.IsFalse(result.Success);
            Assert.AreEqual("Недействителен", result.Message);
        }

        [TestMethod]
        public void ValidateAndApplyPromoCode_EmptyCode_ReturnsError() // Проверяет ошибку при пустом коде
        {
            string code = "";
            int userId = 1;
            decimal originalCost = 200;
            var result = _module.ValidateAndApplyPromoCode(code, userId, originalCost);
            Assert.IsFalse(result.Success);
            Assert.AreEqual("Введите код промокода", result.Message);
        }

        [TestMethod]
        public void ValidateAndApplyPromoCode_DiscountExceedsCost_LimitsDiscount() // Проверяет ограничение скидки стоимостью поездки
        {
            string code = "FIXED50";
            int userId = 1;
            decimal originalCost = 30;
            var result = _module.ValidateAndApplyPromoCode(code, userId, originalCost);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(30.00m, result.DiscountAmount);
            Assert.AreEqual(0.00m, result.FinalCost);
        }

        #endregion

        #region Тесты модуля рейтинга

        [TestMethod]
        public void AddDriverReview_DuplicateReview_Forbidden() // Проверяет запрет на повторное оставление отзыва к одной поездке
        {
            int tripId = 1; 
            int userId = 1;

            var result = _ratingModule.AddDriverReview(tripId, userId, 4, "Повторный отзыв");

            Assert.IsFalse(result.Success);
            Assert.AreEqual("Вы уже оставили отзыв на эту поездку", result.Message);
            Assert.AreEqual(ReviewStatus.AlreadyLeft, result.Status);
        }

        [TestMethod]
        public void CalculateAverageRating_Driver1_HasReview() // Проверяет корректный расчёт среднего рейтинга
        {

            int driverId = 1;

            var avgRating = _ratingModule.CalculateAverageRating(driverId);

            Assert.IsTrue(avgRating > 0 && avgRating <= 5,
                $"Рейтинг должен быть в диапазоне 0-5. Получено: {avgRating}");

            Assert.AreEqual(Math.Round(avgRating, 2), avgRating,
                $"Рейтинг должен быть округлён до 2 знаков");
        }

        [TestMethod]
        public void GetReviewStatusText_CorrectMessages() // Проверяет формирование верных статусных сообщений
        {
            int completedTripId = 1;
            int userId = 1;

            var statusAfterReview = _ratingModule.GetReviewStatusText(completedTripId, userId);
            Assert.AreEqual("Уже оставлен", statusAfterReview);

            var statusNonExistent = _ratingModule.GetReviewStatusText(9999, userId);
            Assert.AreEqual("Недоступен", statusNonExistent);
        }

        [TestMethod]
        public void GetAllDriverRatingsSorted_ReturnsList() // Проверяет возврат не пустого списка и корректную сортировку по убыванию рейтинга
        {
            var ratings = _ratingModule.GetAllDriverRatingsSorted();
            
            Assert.IsNotNull(ratings);
            Assert.IsTrue(ratings.Count > 0);
            
            if (ratings.Count > 1)
            {
                Assert.IsTrue(ratings[0].AverageRating >= ratings[1].AverageRating);
            }
        }

        #endregion
    }
}
