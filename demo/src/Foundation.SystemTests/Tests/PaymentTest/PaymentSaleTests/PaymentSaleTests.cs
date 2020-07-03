using NUnit.Framework;
using Foundation.SystemTests.Tests.Base;
using Foundation.SystemTests.Tests.Helpers;
using System.Collections.Generic;
using Svea.WebPay.SDK;
using System.Threading.Tasks;
using Foundation.SystemTests.Test.Helpers;

namespace Foundation.SystemTests.Tests.PaymentTest.PaymentSaleTests
{
    [Category(TestCategory.Sale)]
    public class PaymentSaleTests : PaymentTests
    {
        public PaymentSaleTests(string driverAlias) : base(driverAlias) { }

        [Test]
        [Category(TestCategory.Swish)]
        [TestCaseSource(nameof(TestData), new object[] { false })]
        public async Task Sale_With_SwishAsync(Product[] products)
        {
            var expected = new List<Dictionary<string, string>>
            {
                new Dictionary<string, string> { { PaymentColumns.TransactionType, TransactionType.Sale }, { PaymentColumns.Status, PaymentStatus.Processed }}
            };


            // Arrange
            GoToThankYouPage(products, paymentMethod: PaymentMethods.Option.Swish);


            // Act
            GoToManagerPage()
                .AssertPaymentOrderTransactions(_orderId, expected, out var paymentOrderLink);

            // Assert
            var order = await _sveaClient.PaymentAdmin.GetOrder(long.Parse(paymentOrderLink));

            // Operations
            Assert.That(order.OrderStatus, Is.EqualTo(Svea.WebPay.SDK.PaymentAdminApi.OrderStatus.Delivered));
            Assert.That(order.PaymentType, Is.EqualTo(Svea.WebPay.SDK.PaymentAdminApi.PaymentType.Swish));
            Assert.That(order.AvailableActions.Count, Is.EqualTo(0));
        }
    }
}
