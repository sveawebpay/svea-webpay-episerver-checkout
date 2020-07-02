using NUnit.Framework;
using Foundation.SystemTests.Tests.Base;
using Foundation.SystemTests.Tests.Helpers;
using System.Collections.Generic;
using System.Threading.Tasks;
using Foundation.SystemTests.Test.Helpers;
using System.Linq;

namespace Foundation.SystemTests.Tests.PaymentTest.PaymentReversalTests
{
    [Category(TestCategory.Reversal)]
    public class PaymentReversalTests : PaymentTests
    {
        public PaymentReversalTests(string driverAlias) : base(driverAlias) { }

        [Test]
        [Category(TestCategory.Card)]
        [TestCaseSource(nameof(TestData), new object[] { false })]
        public async Task PartialReversal_With_CardAsync(Product[] products)
        {
            // "Credit" is the terminology used in EPiServer to qualify a Reversal
            var expected = new List<Dictionary<string, string>>
            {
                new Dictionary<string, string> { { PaymentColumns.TransactionType, TransactionType.Authorization }, { PaymentColumns.Status, PaymentStatus.Processed } },
                new Dictionary<string, string> { { PaymentColumns.TransactionType, TransactionType.Capture       }, { PaymentColumns.Status, PaymentStatus.Processed } },
                new Dictionary<string, string> { { PaymentColumns.TransactionType, TransactionType.Credit        }, { PaymentColumns.Status, PaymentStatus.Processed } },
            };

            GoToThankYouPage(products, paymentMethod: PaymentMethods.Option.Card);

            GoToManagerPage()
                .CreateCapture(_orderId)
                .CreateReversal(_orderId, new Product[] { products[0] }, partial: true)
                .AssertPaymentOrderTransactions(_orderId, expected, out var paymentOrderLink);

            var order = await _sveaClient.PaymentAdmin.GetOrder(long.Parse(paymentOrderLink));

            // Operations
            Assert.That(order.OrderStatus, Is.EqualTo(Svea.WebPay.SDK.PaymentAdminApi.OrderStatus.Delivered));
            Assert.That(order.PaymentType, Is.EqualTo(Svea.WebPay.SDK.PaymentAdminApi.PaymentType.Card));
            Assert.That(order.AvailableActions.Count, Is.EqualTo(2));
            Assert.That(order.AvailableActions, Is.EquivalentTo(new List<string> { "CanCancelOrder", "CanCancelAmount" }));
            Assert.That(order.OrderAmount.Value, Is.EqualTo(_totalAmount * 100));
            Assert.That(order.CancelledAmount.Value, Is.EqualTo(products[0].Quantity * products[0].UnitPrice * 100));
            
            Assert.IsNull(order.OrderRows);

            Assert.That(order.Deliveries.FirstOrDefault().CreditedAmount, Is.EqualTo(0));
            Assert.That(order.Deliveries.FirstOrDefault().DeliveryAmount, Is.EqualTo((_totalAmount * 100) - (products[0].Quantity * products[0].UnitPrice * 100)));
            Assert.IsTrue(order.Deliveries.FirstOrDefault().OrderRows.Any(item => item.Name.ToUpper() == products[0].Name.ToUpper()));
            Assert.IsTrue(order.Deliveries.FirstOrDefault().OrderRows.Any(item => item.Name.ToUpper() == products[1].Name.ToUpper()));
        }

        [Test]
        [Category(TestCategory.Card)]
        [TestCaseSource(nameof(TestData), new object[] { false })]
        public async Task ConsecutivePartialReversal_With_CardAsync(Product[] products)
        {
            // "Credit" is the terminology used in EPiServer to qualify a Reversal
            var expected = new List<Dictionary<string, string>>
            {
                new Dictionary<string, string> { { PaymentColumns.TransactionType, TransactionType.Authorization}, { PaymentColumns.Status, PaymentStatus.Processed } },
                new Dictionary<string, string> { { PaymentColumns.TransactionType, TransactionType.Capture },      { PaymentColumns.Status, PaymentStatus.Processed } },
                new Dictionary<string, string> { { PaymentColumns.TransactionType, TransactionType.Credit },       { PaymentColumns.Status, PaymentStatus.Processed } },
                new Dictionary<string, string> { { PaymentColumns.TransactionType, TransactionType.Credit },       { PaymentColumns.Status, PaymentStatus.Processed } },
            };

            GoToThankYouPage(products, paymentMethod: PaymentMethods.Option.Card);

            GoToManagerPage()
                .CreateCapture(_orderId)
                .CreateReversal(_orderId, new Product[] { products[0] }, partial: true, index: 0)
                .CreateReversal(_orderId, new Product[] { products[1] }, partial: true, index: 1)
                .AssertPaymentOrderTransactions(_orderId, expected, out var paymentOrderLink);

            // Assert
            var order = await _sveaClient.PaymentAdmin.GetOrder(long.Parse(paymentOrderLink));

            // Operations
            Assert.That(order.OrderStatus, Is.EqualTo(Svea.WebPay.SDK.PaymentAdminApi.OrderStatus.Delivered));
            Assert.That(order.PaymentType, Is.EqualTo(Svea.WebPay.SDK.PaymentAdminApi.PaymentType.Card));
            Assert.That(order.AvailableActions.Count, Is.EqualTo(2));
            Assert.That(order.AvailableActions, Is.EquivalentTo(new List<string> { "CanCancelOrder", "CanCancelAmount" }));
            Assert.That(order.OrderAmount.Value, Is.EqualTo(_totalAmount * 100));
            Assert.That(order.CancelledAmount.Value, Is.EqualTo(products.Sum(x => x.Quantity * x.UnitPrice * 100)));

            Assert.IsNull(order.OrderRows);

            Assert.That(order.Deliveries.FirstOrDefault().CreditedAmount, Is.EqualTo(0));
            Assert.That(order.Deliveries.FirstOrDefault().DeliveryAmount, Is.EqualTo( (_totalAmount * 100) - products.Sum(x => x.Quantity * x.UnitPrice * 100)));
            Assert.IsTrue(order.Deliveries.FirstOrDefault().OrderRows.Any(item => item.Name.ToUpper() == products[0].Name.ToUpper()));
            Assert.IsTrue(order.Deliveries.FirstOrDefault().OrderRows.Any(item => item.Name.ToUpper() == products[1].Name.ToUpper()));
        }

        [Test]
        [Category(TestCategory.Card)]
        [TestCaseSource(nameof(TestData), new object[] { false })]
        public async Task FullReversal_With_CardAsync(Product[] products)
        {
            // "Credit" is the terminology used in EPiServer to qualify a Reversal
            var expected = new List<Dictionary<string, string>>
            {
                new Dictionary<string, string> { { PaymentColumns.TransactionType, TransactionType.Authorization }, { PaymentColumns.Status, PaymentStatus.Processed } },
                new Dictionary<string, string> { { PaymentColumns.TransactionType, TransactionType.Capture       }, { PaymentColumns.Status, PaymentStatus.Processed } },
                new Dictionary<string, string> { { PaymentColumns.TransactionType, TransactionType.Credit        }, { PaymentColumns.Status, PaymentStatus.Processed } },
            };

            // Arrange
            GoToThankYouPage(products, paymentMethod: PaymentMethods.Option.Card);


            // Act
            GoToManagerPage()
                .CreateCapture(_orderId)
                .CreateReversal(_orderId, products, partial: false)
                .AssertPaymentOrderTransactions(_orderId, expected, out var paymentOrderLink);

            // Assert
            var order = await _sveaClient.PaymentAdmin.GetOrder(long.Parse(paymentOrderLink));

            // Operations
            Assert.That(order.OrderStatus, Is.EqualTo(Svea.WebPay.SDK.PaymentAdminApi.OrderStatus.Cancelled));
            Assert.That(order.PaymentType, Is.EqualTo(Svea.WebPay.SDK.PaymentAdminApi.PaymentType.Card));
            Assert.That(order.AvailableActions.Count, Is.EqualTo(0));
            Assert.That(order.OrderAmount.Value, Is.EqualTo(_totalAmount * 100));
            Assert.That(order.CancelledAmount.Value, Is.EqualTo(_totalAmount * 100));

            Assert.IsTrue(order.OrderRows.Any(item => item.Name.ToUpper() == products[0].Name.ToUpper()));
            Assert.IsTrue(order.OrderRows.Any(item => item.Name.ToUpper() == products[1].Name.ToUpper()));

            Assert.IsNull(order.Deliveries);
        }
    }
}
