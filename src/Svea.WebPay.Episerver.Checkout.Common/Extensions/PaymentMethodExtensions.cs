﻿using Mediachase.Commerce.Orders.Dto;
using Mediachase.Commerce.Orders.Exceptions;

using System;

namespace Svea.WebPay.Episerver.Checkout.Common.Extensions
{
    internal static class PaymentMethodsExtensions
    {
        internal static string GetParameter(this PaymentMethodDto.PaymentMethodParameterRow row, string name, string defaultValue = null)
        {
            if (row == null || string.IsNullOrWhiteSpace(row.Value))
            {
                if (defaultValue != null)
                    return defaultValue;

                throw new PaymentException(
                    PaymentException.ErrorType.ConfigurationError,
                    "NO_SETTING",
                    "Svea WebPay payment provider: Required setting '" + name + "' is not specified.");
            }
            return row.Value;
        }

        internal static string GetParameter(this PaymentMethodDto paymentMethod, string name, string defaultValue = null)
        {
            var row = GetParameterRow(paymentMethod, name);

            return row.GetParameter(name, defaultValue);
        }

        internal static void SetParameter(this PaymentMethodDto paymentMethod, string name, string value)
        {
            var row = GetParameterRow(paymentMethod, name);
            if (row != null)
            {
                row.Value = value;
            }
            else
            {
                row = paymentMethod.PaymentMethodParameter.NewPaymentMethodParameterRow();
                row.PaymentMethodId = (paymentMethod.PaymentMethod.Count > 0) ? paymentMethod.PaymentMethod[0].PaymentMethodId : Guid.Empty;
                row.Parameter = name;
                row.Value = value;

                paymentMethod.PaymentMethodParameter.Rows.Add(row);
            }
        }

        internal static PaymentMethodDto.PaymentMethodParameterRow GetParameterRow(this PaymentMethodDto paymentMethod, string name)
        {
            var rows = (PaymentMethodDto.PaymentMethodParameterRow[])paymentMethod.PaymentMethodParameter.Select($"Parameter = '{name}'");

            return rows.Length > 0 ? rows[0] : null;
        }
    }
}
