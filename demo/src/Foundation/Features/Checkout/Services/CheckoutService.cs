﻿using EPiServer;
using EPiServer.Commerce.Marketing;
using EPiServer.Commerce.Order;
using EPiServer.Core;
using EPiServer.Framework.Localization;
using EPiServer.Logging;
using EPiServer.Security;
using EPiServer.Web.Routing;
using Foundation.Cms.Settings;
using Foundation.Commerce;
using Foundation.Commerce.Customer.Services;
using Foundation.Features.Checkout.ViewModels;
using Foundation.Features.MyAccount.AddressBook;
using Foundation.Features.Settings;
using Foundation.Features.Shared;
using Mediachase.Commerce.Customers;
using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Orders.Exceptions;
using Mediachase.Commerce.Orders.Managers;
using Mediachase.Commerce.Security;
using Mediachase.Web.Console.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;
using Svea.WebPay.Episerver.Checkout;
using Svea.WebPay.Episerver.Checkout.Common;
using Svea.WebPay.Episerver.Checkout.Common.Extensions;
using Svea.WebPay.SDK.CheckoutApi;
using PaymentType = Mediachase.Commerce.Orders.PaymentType;

namespace Foundation.Features.Checkout.Services
{
    public class CheckoutService
    {
        private readonly IAddressBookService _addressBookService;
        private readonly IOrderGroupCalculator _orderGroupCalculator;
        private readonly IOrderGroupFactory _orderGroupFactory;
        private readonly IPaymentProcessor _paymentProcessor;
        private readonly IOrderRepository _orderRepository;
        private readonly IContentRepository _contentRepository;
        private readonly CustomerContext _customerContext;
        private readonly LocalizationService _localizationService;
        private readonly IMailService _mailService;
        private readonly IPromotionEngine _promotionEngine;
        private readonly ILogger _log = LogManager.GetLogger(typeof(CheckoutService));
        private readonly ILoyaltyService _loyaltyService;
        private readonly ISettingsService _settingsService;
        private readonly ICartService _cartService;
        private readonly ISveaWebPayCheckoutService _sveaWebPayCheckoutService;

        public AuthenticatedPurchaseValidation AuthenticatedPurchaseValidation { get; private set; }
        public AnonymousPurchaseValidation AnonymousPurchaseValidation { get; private set; }
        public CheckoutAddressHandling CheckoutAddressHandling { get; private set; }

        public CheckoutService(
            IAddressBookService addressBookService,
            IOrderGroupFactory orderGroupFactory,
            IOrderGroupCalculator orderGroupCalculator,
            IPaymentProcessor paymentProcessor,
            IOrderRepository orderRepository,
            IContentRepository contentRepository,
            LocalizationService localizationService,
            IMailService mailService,
            IPromotionEngine promotionEngine,
            ILoyaltyService loyaltyService,
            ISettingsService settingsService,
            ICartService cartService,
            ISveaWebPayCheckoutService sveaWebPayCheckoutService)
        {
            _addressBookService = addressBookService;
            _orderGroupFactory = orderGroupFactory;
            _orderGroupCalculator = orderGroupCalculator;
            _paymentProcessor = paymentProcessor;
            _orderRepository = orderRepository;
            _contentRepository = contentRepository;
            _customerContext = CustomerContext.Current;
            _localizationService = localizationService;
            _mailService = mailService;
            _promotionEngine = promotionEngine;
            _loyaltyService = loyaltyService;

            AuthenticatedPurchaseValidation = new AuthenticatedPurchaseValidation(_localizationService);
            AnonymousPurchaseValidation = new AnonymousPurchaseValidation(_localizationService);
            CheckoutAddressHandling = new CheckoutAddressHandling(_addressBookService);
            _settingsService = settingsService;
            _cartService = cartService;
            _sveaWebPayCheckoutService = sveaWebPayCheckoutService;
        }

        public virtual void UpdateShippingMethods(ICart cart, IList<ShipmentViewModel> shipmentViewModels)
        {
            var index = 0;
            foreach (var shipment in cart.GetFirstForm().Shipments)
            {
                shipment.ShippingMethodId = shipmentViewModels[index++].ShippingMethodId;
            }
        }

        public virtual void UpdateShippingAddresses(ICart cart, CheckoutViewModel viewModel)
        {
            var shipments = cart.GetFirstForm().Shipments;
            for (var index = 0; index < shipments.Count; index++)
            {
                shipments.ElementAt(index).ShippingAddress = _addressBookService.ConvertToAddress(viewModel.Shipments[index].Address, cart);
            }
        }

        public virtual void ChangeAddress(ICart cart, CheckoutViewModel viewModel, UpdateAddressViewModel updateAddressViewModel)
        {
            if (updateAddressViewModel.AddressType == AddressType.Billing)
            {
                foreach (var payment in cart.GetFirstForm().Payments)
                {
                    payment.BillingAddress = _addressBookService.ConvertToAddress(viewModel.BillingAddress, cart);
                }
            }
            else
            {
                var shipments = cart.GetFirstForm().Shipments;
                shipments.ElementAt(updateAddressViewModel.ShippingAddressIndex).ShippingAddress =
                        _addressBookService.ConvertToAddress(viewModel.Shipments[updateAddressViewModel.ShippingAddressIndex].Address, cart);
            }
        }

        /// <summary>
        /// Update payment plan information
        /// </summary>
        /// <param name="cart"></param>
        /// <param name="viewModel"></param>
        public virtual void UpdatePaymentPlan(ICart cart, CheckoutViewModel viewModel)
        {
            if (viewModel.IsUsePaymentPlan)
            {
                cart.Properties["IsUsePaymentPlan"] = true;
                cart.Properties["PaymentPlanSetting"] = viewModel.PaymentPlanSetting;
            }
            else
            {
                cart.Properties["IsUsePaymentPlan"] = false;
            }
        }

        public virtual void ApplyDiscounts(ICart cart) => cart.ApplyDiscounts(_promotionEngine, new PromotionEngineSettings());

        public virtual void CreateAndAddPaymentToCart(ICart cart, CheckoutViewModel viewModel)
        {
            var total = viewModel.OrderSummary.PaymentTotal;
            var paymentMethod = viewModel.Payment;
            if (paymentMethod == null)
            {
                return;
            }

            var payment = cart.GetFirstForm().Payments.FirstOrDefault(x => x.PaymentMethodId == paymentMethod.PaymentMethodId);
            if (payment == null)
            {
                payment = paymentMethod.CreatePayment(total, cart);
                cart.AddPayment(payment, _orderGroupFactory);
            }
            else
            {
                payment.Amount = viewModel.OrderSummary.PaymentTotal;
            }
        }

        public virtual void RemovePaymentFromCart(ICart cart, CheckoutViewModel viewModel)
        {
            var paymentMethod = viewModel.Payment;
            if (paymentMethod == null)
            {
                return;
            }

            var payment = cart.GetFirstForm().Payments.FirstOrDefault(x => x.PaymentMethodId == paymentMethod.PaymentMethodId);
            cart.GetFirstForm().Payments.Remove(payment);
        }

        public virtual IPurchaseOrder PlaceOrder(ICart cart, ModelStateDictionary modelState, CheckoutViewModel checkoutViewModel)
        {
            try
            {
                if (cart.Properties[Constant.Quote.ParentOrderGroupId] != null)
                {
                    var orderLink = int.Parse(cart.Properties[Constant.Quote.ParentOrderGroupId].ToString());
                    if (orderLink != 0)
                    {
                        var quoteOrder = _orderRepository.Load<IPurchaseOrder>(orderLink);
                        if (quoteOrder.Properties[Constant.Quote.QuoteStatus] != null)
                        {
                            checkoutViewModel.QuoteStatus = quoteOrder.Properties[Constant.Quote.QuoteStatus].ToString();
                            if (quoteOrder.Properties[Constant.Quote.QuoteStatus].ToString().Equals(Constant.Quote.RequestQuotationFinished))
                            {
                                _ = DateTime.TryParse(quoteOrder.Properties[Constant.Quote.QuoteExpireDate].ToString(),
                                    out var quoteExpireDate);
                                if (DateTime.Compare(DateTime.Now, quoteExpireDate) > 0)
                                {
                                    _orderRepository.Delete(cart.OrderLink);
                                    _orderRepository.Delete(quoteOrder.OrderLink);
                                    throw new InvalidOperationException("Quote Expired");
                                }
                            }
                        }
                    }
                }

                var processPayments = cart.ProcessPayments(_paymentProcessor, _orderGroupCalculator);
                var unsuccessPayments = processPayments.Where(x => !x.IsSuccessful);
                if (unsuccessPayments != null && unsuccessPayments.Any())
                {
                    throw new InvalidOperationException(string.Join("\n", unsuccessPayments.Select(x => x.Message)));
                }

                var processedPayments = cart.GetFirstForm().Payments.Where(x => x.Status.Equals(PaymentStatus.Processed.ToString()));

                if (!processedPayments.Any())
                {
                    // Return null in case there is no payment was processed.
                    return null;
                }

                var totalProcessedAmount = processedPayments.Sum(x => x.Amount);
                if (totalProcessedAmount != cart.GetTotal(_orderGroupCalculator).Amount)
                {
                    throw new InvalidOperationException("Wrong amount");
                }

                var orderReference = (cart.Properties["IsUsePaymentPlan"] != null && cart.Properties["IsUsePaymentPlan"].Equals(true)) ? SaveAsPaymentPlan(cart) : _orderRepository.SaveAsPurchaseOrder(cart);
                var purchaseOrder = _orderRepository.Load<IPurchaseOrder>(orderReference.OrderGroupId);
                _orderRepository.Delete(cart.OrderLink);

                cart.AdjustInventoryOrRemoveLineItems((item, validationIssue) => { });

                //Loyalty Program: Add Points and Number of orders
                _loyaltyService.AddNumberOfOrders();

                return purchaseOrder;
            }
            catch (PaymentException ex)
            {
                modelState.AddModelError("", _localizationService.GetString("/Checkout/Payment/Errors/ProcessingPaymentFailure") + ex.Message);
            }
            catch (Exception ex)
            {
                modelState.AddModelError("", ex.Message);
            }

            return null;
        }

        public virtual async Task<bool> SendConfirmation(CheckoutViewModel viewModel, IPurchaseOrder purchaseOrder)
        {
            var referenceSettings = _settingsService.GetSiteSettings<ReferencePageSettings>();
            var sendOrderConfirmationMail = referenceSettings?.SendOrderConfirmationMail ?? false;
            if (sendOrderConfirmationMail)
            {
                var queryCollection = new NameValueCollection
                {
                    {"contactId", _customerContext.CurrentContactId.ToString()},
                    {"orderNumber", purchaseOrder.OrderLink.OrderGroupId.ToString(CultureInfo.CurrentCulture)}
                };

                try
                {
                    await _mailService.SendAsync(referenceSettings.OrderConfirmationMail, queryCollection, purchaseOrder.GetFirstForm().Payments.FirstOrDefault().BillingAddress.Email, CultureInfo.CurrentCulture.Name);
                }
                catch (Exception e)
                {
                    _log.Warning(string.Format("Unable to send purchase receipt to '{0}'.", purchaseOrder.GetFirstForm().Payments.FirstOrDefault().BillingAddress.Email), e);
                    return false;
                }
            }

            return true;
        }

        public virtual string BuildRedirectionUrl(CheckoutViewModel checkoutViewModel, IPurchaseOrder purchaseOrder, bool confirmationSentSuccessfully)
        {
            var queryCollection = new NameValueCollection
            {
                {"contactId", _customerContext.CurrentContactId.ToString()},
                {"orderNumber", purchaseOrder.OrderLink.OrderGroupId.ToString(CultureInfo.CurrentCulture)}
            };

            if (!confirmationSentSuccessfully)
            {
                queryCollection.Add("notificationMessage", string.Format(_localizationService.GetString("/OrderConfirmationMail/ErrorMessages/SmtpFailure"), checkoutViewModel.BillingAddress?.Email));
            }

            var referenceSettings = _settingsService.GetSiteSettings<ReferencePageSettings>();
            var confirmationPage = referenceSettings?.OrderConfirmationPage ?? ContentReference.EmptyReference;
            if (ContentReference.IsNullOrEmpty(confirmationPage))
            {
                return null;
            }

            return new UrlBuilder(UrlResolver.Current.GetUrl(confirmationPage)) { QueryCollection = queryCollection }.ToString();
        }

        public void ProcessPaymentCancel(CheckoutViewModel viewModel, TempDataDictionary tempData, ControllerContext controlerContext)
        {
            var message = tempData["message"] != null ? tempData["message"].ToString() : controlerContext.HttpContext.Request.QueryString["message"];
            if (!string.IsNullOrEmpty(message))
            {
                viewModel.Message = message;
            }
        }

        #region Payment Plan

        /// <summary>
        /// Save cart as payment plan
        /// </summary>
        /// <param name="cart"></param>
        private OrderReference SaveAsPaymentPlan(ICart cart)
        {
            var orderReference = _orderRepository.SaveAsPaymentPlan(cart);
            var paymentPlanSetting = cart.Properties["PaymentPlanSetting"] as PaymentPlanSetting;

            IPaymentPlan paymentPlan;
            paymentPlan = _orderRepository.Load<IPaymentPlan>(orderReference.OrderGroupId);
            paymentPlan.CycleMode = PaymentPlanCycle.Days;
            paymentPlan.CycleLength = paymentPlanSetting.CycleLength;
            paymentPlan.StartDate = DateTime.Now.AddDays(paymentPlanSetting.CycleLength);
            paymentPlan.EndDate = paymentPlanSetting.EndDate;
            paymentPlan.IsActive = paymentPlanSetting.IsActive;

            var principal = PrincipalInfo.CurrentPrincipal;
            AddNoteToCart(paymentPlan, $"Note: New payment plan placed by {principal.Identity.Name} in 'vnext site'.", OrderNoteTypes.System.ToString(), principal.GetContactId());

            _orderRepository.Save(paymentPlan);

            paymentPlan.AdjustInventoryOrRemoveLineItems((item, validationIssue) => { });
            _orderRepository.Save(paymentPlan);

            //create first order
            orderReference = _orderRepository.SaveAsPurchaseOrder(paymentPlan);
            var purchaseOrder = _orderRepository.Load(orderReference);
            OrderGroupWorkflowManager.RunWorkflow((OrderGroup)purchaseOrder, OrderGroupWorkflowManager.CartCheckOutWorkflowName);
            var noteDetailPattern = "New purchase order placed by {0} in {1} from payment plan {2}";
            var noteDetail = string.Format(noteDetailPattern, ManagementHelper.GetUserName(PrincipalInfo.CurrentPrincipal.GetContactId()), "VNext site", (paymentPlan as PaymentPlan).Id);
            AddNoteToPurchaseOrder(purchaseOrder as IPurchaseOrder, noteDetail, OrderNoteTypes.System, PrincipalInfo.CurrentPrincipal.GetContactId());
            _orderRepository.Save(purchaseOrder);

            paymentPlan.LastTransactionDate = DateTime.UtcNow;
            paymentPlan.CompletedCyclesCount++;
            _orderRepository.Save(paymentPlan);

            return orderReference;
        }

        /// <summary>
        /// Add note to purchase order
        /// </summary>
        /// <param name="purchaseOrder"></param>
        /// <param name="noteDetails"></param>
        /// <param name="type"></param>
        /// <param name="customerId"></param>
        private void AddNoteToPurchaseOrder(IPurchaseOrder purchaseOrder, string noteDetails, OrderNoteTypes type, Guid customerId)
        {
            if (purchaseOrder == null)
            {
                throw new ArgumentNullException(nameof(purchaseOrder));
            }

            var orderNote = purchaseOrder.CreateOrderNote();

            if (!orderNote.OrderNoteId.HasValue)
            {
                var newOrderNoteId = -1;

                if (purchaseOrder.Notes.Count != 0)
                {
                    newOrderNoteId = Math.Min(purchaseOrder.Notes.ToList().Min(n => n.OrderNoteId.Value), 0) - 1;
                }

                orderNote.OrderNoteId = newOrderNoteId;
            }

            orderNote.CustomerId = customerId;
            orderNote.Type = type.ToString();
            orderNote.Title = noteDetails.Substring(0, Math.Min(noteDetails.Length, 24)) + "...";
            orderNote.Detail = noteDetails;
            orderNote.Created = DateTime.UtcNow;
        }

        /// <summary>
        /// Add note to cart
        /// </summary>
        /// <param name="cart"></param>
        /// <param name="noteDetails"></param>
        /// <param name="type"></param>
        /// <param name="originator"></param>
        private void AddNoteToCart(IOrderGroup cart, string noteDetails, string type, Guid originator)
        {
            var note = new OrderNote
            {
                CustomerId = originator,
                Type = type,
                Title = noteDetails.Substring(0, Math.Min(noteDetails.Length, 24)) + "...",
                Detail = noteDetails,
                Created = DateTime.UtcNow
            };
            cart.Notes.Add(note);
        }
        #endregion
        protected static readonly ConcurrentDictionary<string, DateTime> ProcessingOrdersCache = new ConcurrentDictionary<string, DateTime>();

        public IPurchaseOrder GetOrCreatePurchaseOrder(int orderGroupId, long sveaWebPayOrderId, out HttpStatusCode status)
        {
            var key = $"{orderGroupId}-{sveaWebPayOrderId}";
            if (ProcessingOrdersCache.TryAdd(key, DateTime.UtcNow))
            {
                // Check if the order has been created already
                var purchaseOrder = _sveaWebPayCheckoutService.GetPurchaseOrderBySveaWebPayOrderId(sveaWebPayOrderId.ToString());
                if (purchaseOrder != null)
                {
                    status = HttpStatusCode.OK;
                    ProcessingOrdersCache.TryRemove(key, out DateTime value1);
                    return purchaseOrder;
                }

                // Check if we still have a cart and can create an order
                var cart = _orderRepository.Load<ICart>(orderGroupId);
                if (cart == null)
                {
                    _log.Log(Level.Information, $"Purchase order or cart with orderId {orderGroupId} not found");
                    status = HttpStatusCode.NotFound;
                    ProcessingOrdersCache.TryRemove(key, out DateTime value2);
                    return null;
                }

                var cartSveaWebPayOrderId = cart.Properties[Constants.SveaWebPayOrderIdField]?.ToString();
                if (cartSveaWebPayOrderId == null || !cartSveaWebPayOrderId.Equals(sveaWebPayOrderId.ToString()))
                {
                    _log.Log(Level.Information, $"cart: {orderGroupId} with svea webpay order id {cartSveaWebPayOrderId} does not equal svea webpay order id {sveaWebPayOrderId} sent in the request");
                    status = HttpStatusCode.Conflict;
                    ProcessingOrdersCache.TryRemove(key, out DateTime value3);
                    return null;
                }

                var order = _sveaWebPayCheckoutService.GetOrder(cart);
                if (!order.Status.Equals(CheckoutOrderStatus.Final))
                {
                    // Won't create order, Svea webpay checkout not complete
                    _log.Log(Level.Information, $"Svea webpay order id {cartSveaWebPayOrderId} not completed");
                    status = HttpStatusCode.NotFound;
                    ProcessingOrdersCache.TryRemove(key, out DateTime value4);
                    return null;
                }

                purchaseOrder = CreatePurchaseOrderForSveaWebPay(sveaWebPayOrderId, order, cart);
                status = HttpStatusCode.OK;
                ProcessingOrdersCache.TryRemove(key, out DateTime value5);
                return purchaseOrder;
            }

            _log.Log(Level.Information, $"Already processing orderGroupId: {orderGroupId} orderId: {sveaWebPayOrderId}");
            status = HttpStatusCode.Conflict;
            ProcessingOrdersCache.TryRemove(key, out DateTime value6);
            return null;
        }

        public IPurchaseOrder CreatePurchaseOrderForSveaWebPay(long sveaWebPayOrderId, Data order, ICart cart)
        {
            // Clean up payments in cart on payment provider site.
            foreach (var form in cart.Forms)
            {
                form.Payments.Clear();
            }

            var languageid = cart.Properties[Constants.Culture].ToString();
            var paymentRow = PaymentManager.GetPaymentMethodBySystemName(Constants.SveaWebPayCheckoutSystemKeyword, languageid, cart.MarketId.Value).PaymentMethod.FirstOrDefault();

            var payment = cart.CreatePayment(_orderGroupFactory);
            payment.PaymentType = PaymentType.Other;
            payment.PaymentMethodId = paymentRow.PaymentMethodId;
            payment.PaymentMethodName = Constants.SveaWebPayCheckoutSystemKeyword;
            payment.Amount = cart.GetTotal(_orderGroupCalculator).Amount;

            var isSaleTransaction = order.Payment.PaymentMethodType == PaymentMethodType.DirectBank || order.Payment.PaymentMethodType == PaymentMethodType.Trustly || order.Payment.PaymentMethodType == PaymentMethodType.Swish;

            payment.Status = isSaleTransaction
                ? PaymentStatus.Processed.ToString()
                : PaymentStatus.Pending.ToString();

            payment.TransactionType = isSaleTransaction
                ? TransactionType.Sale.ToString()
                : TransactionType.Authorization.ToString();

            cart.AddPayment(payment, _orderGroupFactory);
            cart.AddNote($"Payed with {order.Payment?.PaymentMethodType?.ToString()}", $"Payed with {order.Payment?.PaymentMethodType?.ToString()}");

            var billingAddress = new AddressModel
            {
                Name = $"{order.BillingAddress.StreetAddress}{order.BillingAddress.PostalCode}{order.BillingAddress.City}",
                FirstName = order.BillingAddress.FullName,
                LastName = order.BillingAddress.LastName,
                Email = order.EmailAddress,
                DaytimePhoneNumber = order.PhoneNumber,
                Line1 = order.BillingAddress.StreetAddress,
                PostalCode = order.BillingAddress.PostalCode,
                City = order.BillingAddress.City,
                CountryCode = order.BillingAddress.CountryCode
            };

            payment.BillingAddress = _addressBookService.ConvertToAddress(billingAddress, cart);

            var shippingAddress = new AddressModel
            {
                Name = $"{order.ShippingAddress.StreetAddress}{order.ShippingAddress.PostalCode}{order.ShippingAddress.City}",
                FirstName = order.ShippingAddress.FullName,
                LastName = order.ShippingAddress.LastName,
                Email = order.EmailAddress,
                DaytimePhoneNumber = order.PhoneNumber,
                Line1 = order.ShippingAddress.StreetAddress,
                PostalCode = order.ShippingAddress.PostalCode,
                City = order.ShippingAddress.City,
                CountryName = order.ShippingAddress.CountryCode,
                CountryCode = cart.GetFirstShipment().ShippingAddress?.CountryCode,
            };

            cart.GetFirstShipment().ShippingAddress = _addressBookService.ConvertToAddress(shippingAddress, cart);

            cart.ProcessPayments(_paymentProcessor, _orderGroupCalculator);

            var totalProcessedAmount = cart.GetFirstForm().Payments.Where(x => x.Status.Equals(PaymentStatus.Processed.ToString())).Sum(x => x.Amount);
            if (totalProcessedAmount != cart.GetTotal(_orderGroupCalculator).Amount)
            {
                throw new InvalidOperationException("Wrong amount");
            }

            _cartService.RequestInventory(cart);

            var orderReference = _orderRepository.SaveAsPurchaseOrder(cart);
            var purchaseOrder = _orderRepository.Load<IPurchaseOrder>(orderReference.OrderGroupId);
            _orderRepository.Delete(cart.OrderLink);

            if (purchaseOrder == null)
            {
                return null;
            }
            else
            {
                purchaseOrder.Properties[Constants.SveaWebPayOrderIdField] = sveaWebPayOrderId;
                _orderRepository.Save(purchaseOrder);
                return purchaseOrder;
            }
        }
    }
}