using NUnit.Framework;
using Foundation.SystemTests.Tests.Base;
using Foundation.SystemTests.Tests.Helpers;
using System.Collections.Generic;
using System.Threading.Tasks;
using Foundation.SystemTests.Test.Helpers;
using System;
using System.Linq;

namespace Foundation.SystemTests.Tests.PaymentTest.PaymentCaptureTests
{
    [Category(TestCategory.Capture)]
    public class PaymentCaptureTests : PaymentTests
    {
        public PaymentCaptureTests(string driverAlias) : base(driverAlias) { }

        [Test]
        [Category(TestCategory.Card)]
        [TestCaseSource(nameof(TestData), new object[] { false })]
        public async Task Capture_With_CardAsync(Product[] products)
        {
            var expected = new List<Dictionary<string, string>>
            {
                new Dictionary<string, string> { { PaymentColumns.TransactionType, TransactionType.Authorization }, { PaymentColumns.Status, PaymentStatus.Processed } },
                new Dictionary<string, string> { { PaymentColumns.TransactionType, TransactionType.Capture },       { PaymentColumns.Status, PaymentStatus.Processed } },
            };

            // Arrange
            GoToThankYouPage(products, paymentMethod: PaymentMethods.Option.Card);


            // Act
            GoToManagerPage()
                .CreateCapture(_orderId)
                .AssertPaymentOrderTransactions(_orderId, expected, out var paymentOrderLink);


            // Assert
            var order = await _sveaClient.PaymentAdmin.GetOrder(long.Parse(paymentOrderLink));

            // Operations
            Assert.That(order.OrderStatus, Is.EqualTo(Svea.WebPay.SDK.PaymentAdminApi.OrderStatus.Delivered));
            Assert.That(order.PaymentType, Is.EqualTo(Svea.WebPay.SDK.PaymentAdminApi.PaymentType.Card));
            Assert.That(order.AvailableActions, Is.EquivalentTo(new List<string> { "CanCancelOrder", "CanCancelAmount" }));
            Assert.That(order.OrderAmount, Is.EqualTo(_totalAmount));

            Assert.IsNull(order.OrderRows);
            
            Assert.That(order.Deliveries.FirstOrDefault().DeliveryAmount, Is.EqualTo(_totalAmount * 100));
            Assert.That(order.Deliveries.FirstOrDefault().CreditedAmount, Is.EqualTo(0));
            Assert.IsTrue(order.Deliveries.FirstOrDefault().OrderRows.Any(item => item.Name.ToUpper() == products[0].Name.ToUpper()));
            Assert.IsTrue(order.Deliveries.FirstOrDefault().OrderRows.Any(item => item.Name.ToUpper() == products[1].Name.ToUpper()));
        }
    }
}
