using NUnit.Framework;
using Foundation.SystemTests.Tests.Base;
using Foundation.SystemTests.Tests.Helpers;
using System.Collections.Generic;
using Svea.WebPay.SDK;
using System.Threading.Tasks;
using System.Linq;

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

            //// Global Order
            //Assert.That(order.PaymentOrderResponse.Amount.Value, Is.EqualTo(double.Parse(_totalAmount) * 100));
            //Assert.That(order.PaymentOrderResponse.Currency.ToString(), Is.EqualTo("SEK"));
            //Assert.That(order.PaymentOrderResponse.State, Is.EqualTo(State.Ready));

            //// Operations
            //Assert.That(order.Operations[LinkRelation.CreatePaymentOrderReversal], Is.Null);
            //Assert.That(order.Operations[LinkRelation.CreateCancellation], Is.Not.Null);
            //Assert.That(order.Operations[LinkRelation.CreatePaymentOrderCapture], Is.Not.Null);
            //Assert.That(order.Operations[LinkRelation.PaidPaymentOrder], Is.Not.Null);

            //// Transactions
            //Assert.That(order.PaymentOrderResponse.CurrentPayment.Payment.Transactions.TransactionList.Count, Is.EqualTo(1));
            //Assert.That(order.PaymentOrderResponse.CurrentPayment.Payment.Transactions.TransactionList.First(x => x.Type == TransactionType.Authorization).State,
            //            Is.EqualTo(State.Completed));

            //// Order Items
            //Assert.That(order.PaymentOrderResponse.OrderItems.OrderItemList.Count, Is.EqualTo(products.Count() + 1));
            //for (var i = 0; i < products.Count(); i++)
            //{
            //    Assert.That(order.PaymentOrderResponse.OrderItems.OrderItemList.ElementAt(i).Name, Is.EqualTo(products[i].Name));
            //    Assert.That(order.PaymentOrderResponse.OrderItems.OrderItemList.ElementAt(i).UnitPrice.Value, Is.EqualTo(products[i].UnitPrice * 100));
            //    Assert.That(order.PaymentOrderResponse.OrderItems.OrderItemList.ElementAt(i).Quantity, Is.EqualTo(products[i].Quantity));
            //    Assert.That(order.PaymentOrderResponse.OrderItems.OrderItemList.ElementAt(i).Amount.Value, Is.EqualTo(products[i].UnitPrice * 100 * products[i].Quantity));
            //}
        }
    }
}
