using Foundation.SystemTests.Tests.Base;
using Foundation.SystemTests.Tests.Helpers;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Foundation.SystemTests.Tests.PaymentTest
{
    [Category(TestCategory.Authorization)]
    public class PaymentAuthorizationTests : PaymentTests
    {
        public PaymentAuthorizationTests(string driverAlias) : base(driverAlias) { }

        [Test]
        [Category(TestCategory.Card)]
        [TestCaseSource(nameof(TestData), new object[] { false })]
        public async Task Authorization_With_CardAsync(Product[] products)
        {
            var expected = new List<Dictionary<string, string>>
            {
                new Dictionary<string, string> { { PaymentColumns.TransactionType, TransactionType.Authorization }, { PaymentColumns.Status, PaymentStatus.Processed }}
            };

            // Arrange
            GoToThankYouPage(products, paymentMethod: Test.Helpers.PaymentMethods.Option.Card);


            // Act
            GoToManagerPage()
                .AssertPaymentOrderTransactions(_orderId, expected, out var paymentOrderLink);


            // Assert
            var order = await _sveaClient.PaymentAdmin.GetOrder(long.Parse(paymentOrderLink));

            // Operations
            Assert.That(order.OrderStatus, Is.EqualTo(Svea.WebPay.SDK.PaymentAdminApi.OrderStatus.Open));
            Assert.That(order.PaymentType, Is.EqualTo(Svea.WebPay.SDK.PaymentAdminApi.PaymentType.Card));
            Assert.That(order.AvailableActions, Is.EquivalentTo(new List<string> { "CanDeliverOrder", "CanCancelOrder", "CanCancelAmount" }));
            Assert.That(order.CancelledAmount.InLowestMonetaryUnit, Is.EqualTo(0));

            Assert.That(order.OrderRows.Count, Is.EqualTo(products.Count() + 1));

            for (var i = 0; i < products.Count(); i++)
            {
                var orderRow = order.OrderRows.ElementAt(i);
                Assert.That(orderRow.Name.ToUpper(), Is.EqualTo(products[i].Name.ToUpper()));
                Assert.That(orderRow.Quantity, Is.EqualTo(products[i].Quantity));
                Assert.That(orderRow.UnitPrice.InLowestMonetaryUnit, Is.EqualTo((products[i].UnitPrice + products[i].UnitPrice * 0.25m) * 100));
            }

            Assert.IsNull(order.Deliveries);
        }
    }
}
