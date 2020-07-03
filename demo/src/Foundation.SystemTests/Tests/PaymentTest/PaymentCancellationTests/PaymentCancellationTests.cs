using NUnit.Framework;
using Foundation.SystemTests.Tests.Base;
using Foundation.SystemTests.Tests.Helpers;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace Foundation.SystemTests.Tests.PaymentTest.PaymentCancellationTests
{
    [Category(TestCategory.Cancellation)]
    public class PaymentCancellationTests : PaymentTests
    {
        public PaymentCancellationTests(string driverAlias) : base(driverAlias) { }

        [Test]
        [Category(TestCategory.Card)]
        [TestCaseSource(nameof(TestData), new object[] { false })]
        public async Task Cancellation_With_CardAsync(Product[] products)
        {
            // "Void" is the terminology used in EPiServer to qualify a cancellation
            var expected = new List<Dictionary<string, string>>
            {
                new Dictionary<string, string> { { PaymentColumns.TransactionType, TransactionType.Authorization }, { PaymentColumns.Status, PaymentStatus.Processed }},
                new Dictionary<string, string> { { PaymentColumns.TransactionType, TransactionType.Cancellation },  { PaymentColumns.Status, PaymentStatus.Processed }}
            };

            // Arrange
            GoToThankYouPage(products);


            // Act
            GoToManagerPage()
                .CancelOrder(_orderId)
                .AssertPaymentOrderTransactions(_orderId, expected, out var paymentOrderLink);


            // Assert
            var order = await _sveaClient.PaymentAdmin.GetOrder(long.Parse(paymentOrderLink));

            // Operations
            Assert.That(order.OrderStatus, Is.EqualTo(Svea.WebPay.SDK.PaymentAdminApi.OrderStatus.Cancelled));
            Assert.That(order.PaymentType, Is.EqualTo(Svea.WebPay.SDK.PaymentAdminApi.PaymentType.Card));
            Assert.That(order.AvailableActions.Count, Is.EqualTo(0));
            Assert.That(order.CancelledAmount.Value, Is.EqualTo(_totalAmount * 100));

            Assert.IsTrue(order.OrderRows.Any(item => item.Name.ToUpper() == products[0].Name.ToUpper()));
            Assert.IsTrue(order.OrderRows.Any(item => item.Name.ToUpper() == products[1].Name.ToUpper()));
            
            Assert.IsNull(order.Deliveries);
        }
    }
}
