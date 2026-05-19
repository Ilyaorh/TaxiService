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

        [TestInitialize]
        public void Setup()
        {
            _module = new TaxiRideModule();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _module?.Dispose();
        }

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
            int driverId = 3;

            bool result = _module.IsDriverAvailable(driverId);

            Assert.IsFalse(result);
        }

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
    }
}