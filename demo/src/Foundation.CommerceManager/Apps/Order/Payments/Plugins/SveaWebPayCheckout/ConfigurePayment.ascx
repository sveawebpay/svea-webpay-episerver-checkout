<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="ConfigurePayment.ascx.cs" Inherits="Svea.WebPay.Episerver.Checkout.CommerceManager.Apps.Order.Payments.Plugins.SveaWebPayCheckout.ConfigurePayment" %>
<%@ Register TagPrefix="mc" Namespace="Mediachase.BusinessFoundation" Assembly="Mediachase.BusinessFoundation, Version=10.4.3.0, Culture=neutral, PublicKeyToken=41d2e7a615ba286c" %>
<%@ Register Assembly="Mediachase.WebConsoleLib" Namespace="Mediachase.Web.Console.Controls" TagPrefix="console" %>


<asp:UpdatePanel UpdateMode="Conditional" ID="ConfigureUpdatePanelContentPanel" runat="server" RenderMode="Inline" ChildrenAsTriggers="true">
	<ContentTemplate>

		<style>
			.sveawebpaypayment-parameters table.DataForm tbody tr td.FormLabelCell {
				width: 200px;
			}

			.sveawebpaypayment-parameters h2 {
				margin-top: 20px
			}

			.sveawebpaypayment-parameters-url {
				width: 500px;
			}

			.sveawebpaypayment-list {
				list-style: disc;
				padding: 10px;
			}

			.sveawebpaypayment-table tr {
				vertical-align: top;
			}

			.pnl_warning {
				margin-top: 20px;
			}

			.sveawebpaypayment-warning {
				color: Red;
				background-color: #DFDFDF;
				font-weight: bold;
				padding: 6px;
				text-align: left;
			}
		</style>

		<div class="sveawebpaypayment-parameters">

			<div>
				<h2>Prerequisites</h2>
				<p>Before you can start integrating Svea WebPay Checkout you need to have the following in place:</p>

				<ul class="sveawebpaypayment-list">
					<li>HTTPS enabled web server</li>
					<li>Obtained credentials (merchant id and secret) from Svea WebPay through Svea.</li>
				</ul>

				<p>If you're missing either of these, please contact <a href="mailto:support-webpay@sveaekonomi.se">support-webpay@sveaekonomi.se</a> for assistance.</p>
				<asp:Panel runat="server" ID="pnl_marketselected" Visible="False" CssClass="pnl_warning">
					<p class="sveawebpaypayment-warning">Before you can set parameters you have to set a market under the tab Markets and click OK </p>
				</asp:Panel>
			</div>

			<asp:Panel runat="server" ID="pnl_parameters">

				<h2>Market</h2>
				<table class="DataForm">
					<tbody>
						<tr>
							<td class="FormLabelCell">Select a market:</td>
							<td class="FormFieldCell">
								<asp:DropDownList runat="server" ID="marketDropDownList" OnSelectedIndexChanged="marketDropDownList_OnSelectedIndexChanged" AutoPostBack="True" />
							</td>
						</tr>
					</tbody>
				</table>

				<h2>Checkout/Checkin</h2>

				<h2>Svea WebPay connection settings</h2>
				<table class="DataForm">
					<tbody>

						<tr>
							<td class="FormLabelCell">CheckoutApi Uri:</td>
							<td class="FormFieldCell">
								<asp:TextBox runat="server" ID="txtCheckoutApiUri" CssClass="sveawebpaypayment-parameters-url" />
								<asp:RequiredFieldValidator ID="requiredApiUrl" runat="server" ControlToValidate="txtCheckoutApiUri" ErrorMessage="CheckoutApi Uri is required." />
								<asp:RegularExpressionValidator runat="server"
									ControlToValidate="txtCheckoutApiUri"
									ValidationExpression="^((https)://)?([\w-]+\.)+[\w]+(/[\w- ./?]*)?$"
									Text="Enter a valid URL" />
							</td>
						</tr>
						<tr>
							<td class="FormLabelCell">PaymentAdminApi Uri:</td>
							<td class="FormFieldCell">
								<asp:TextBox runat="server" ID="txtPaymentAdminApiUri" CssClass="sveawebpaypayment-parameters-url" />
								<asp:RequiredFieldValidator ID="RequiredFieldValidator1" runat="server" ControlToValidate="txtPaymentAdminApiUri" ErrorMessage="PaymentAdminApi Uri is required." />
								<asp:RegularExpressionValidator runat="server"
									ControlToValidate="txtPaymentAdminApiUri"
									ValidationExpression="^((https)://)?([\w-]+\.)+[\w]+(/[\w- ./?]*)?$"
									Text="Enter a valid URL" />
							</td>
						</tr>
						<tr>
							<td class="FormLabelCell">Merchant Id:</td>
							<td class="FormFieldCell">
								<asp:TextBox runat="server" ID="txtMerchantId" CssClass="sveawebpaypayment-parameters-url" />
								<asp:RequiredFieldValidator ID="requiredMerchantId" runat="server" ControlToValidate="txtMerchantId" ErrorMessage="Merchant Id is required." />
							</td>
						</tr>
						<tr>
							<td class="FormLabelCell">Secret:</td>
							<td class="FormFieldCell">
								<asp:TextBox runat="server" ID="txtSecret" CssClass="sveawebpaypayment-parameters-url" />
								<asp:RequiredFieldValidator ID="requiredSecret" runat="server" ControlToValidate="txtSecret" ErrorMessage="Secret is required." />
							</td>
						</tr>
					
                        <tr>
                            <td class="FormLabelCell">
                                <p>Disable reuse of Http Connections to Svea SDK: </p>
                                <i></i>
                            </td>
                            <td class="FormFieldCell">
                                <asp:CheckBox runat="server" ID="chkConnectionClose" CssClass="" />
                            </td>
                        </tr>

					</tbody>
				</table>





				<h2>Merchant settings</h2>
				<table class="DataForm sveawebpaypayment-table">
					<tbody>
						<tr>
							<td class="FormLabelCell">
								<p>Push Uri: </p>
								<i>URI to a location that expects callbacks from the Checkout whenever an order’s state is changed (confirmed, final, etc.).

                                    May contain a {checkout.order.uri} placeholder which will be replaced with the checkoutorderid.
								</i>
							</td>
							<td class="FormFieldCell">
								<asp:TextBox runat="server" ID="txtPushUri" CssClass="sveawebpaypayment-parameters-url" />
								<asp:RequiredFieldValidator ID="requiredHostUrl" runat="server" ControlToValidate="txtPushUri" ErrorMessage="Push Uri is required." />
							</td>
						</tr>


						<tr>
							<td class="FormLabelCell">
								<p>Terms Uri: </p>
								<i>URI to a page with webshop specific terms.	</i>
							</td>
							<td class="FormFieldCell">
								<asp:TextBox runat="server" ID="txtTermsUri" CssClass="sveawebpaypayment-parameters-url" />
								<asp:RequiredFieldValidator ID="requiredTermsOfServiceUrl" runat="server" ControlToValidate="txtTermsUri" ErrorMessage="Terms Uri is required." />
								<asp:RegularExpressionValidator ID="regTermsOfServiceUrl" runat="server"
									ControlToValidate="txtTermsUri"
									ValidationExpression="^((https)://)?([\w-]+\.)+[\w]+(/[\w-={} ./?&]*)?$"
									Text="Enter a valid URL" />
							</td>
						</tr>


						<tr>
							<td class="FormLabelCell">
								<p>Checkout Uri: </p>
								<i>URI to the page in the webshop displaying the Checkout. May not contain order specific information.	</i>
							</td>
							<td class="FormFieldCell">
								<asp:TextBox runat="server" ID="txtCheckoutUri" CssClass="sveawebpaypayment-parameters-url" />
								<asp:RequiredFieldValidator ID="requiredCompleteUrl" runat="server" ControlToValidate="txtCheckoutUri" ErrorMessage="Checkout Uri is required." />
								<asp:RegularExpressionValidator ID="regCompleteUrl" runat="server"
									ControlToValidate="txtCheckoutUri"
									ValidationExpression="^((https)://)?([\w-]+\.)+[\w]+(/[\w-={} ./?&]*)?$"
									Text="Enter a valid URL" />
							</td>
						</tr>
						<tr>
							<td class="FormLabelCell">
								<p>ConfirmationUri Uri: </p>
								<i>URI to the page in the webshop displaying specific information to a customer after the order has been confirmed. May not contain order specific information.</i>
							</td>
							<td class="FormFieldCell">
								<asp:TextBox runat="server" ID="txtConfirmationUri" CssClass="sveawebpaypayment-parameters-url" />
								<asp:RegularExpressionValidator ID="regCancelUrl" runat="server"
									ControlToValidate="txtConfirmationUri"
									ValidationExpression="^((https)://)?([\w-]+\.)+[\w]+(/[\w-={} ./?&]*)?$"
									Text="Enter a valid URL" />

							</td>
						</tr>
						<tr>
							<td class="FormLabelCell">
								<p>Checkout Validation Callback Uri: </p>
								<i>An optional URI to a location that expects callbacks from the Checkout to validate an order’s stock status It also has the possibility to update the checkout with an updated ClientOrderNumber.

                                    May contain a {checkout.order.uri} placeholder which will be replaced with the checkoutorderid.
								</i>
							</td>
							<td class="FormFieldCell">
								<asp:TextBox runat="server" ID="txtCheckoutValidationCallbackUri" CssClass="sveawebpaypayment-parameters-url" />
								<asp:RegularExpressionValidator ID="regCallbackUrl" runat="server"
									ControlToValidate="txtCheckoutValidationCallbackUri"
									ValidationExpression="^((https)://)?([\w-]+\.)+[\w]+(/[\w-={} ./?&]*)?$"
									Text="Enter a valid URL" />
							</td>
						</tr>

						<tr>
							<td class="FormLabelCell">
								<p>Active Part Payment Campaigns: </p>
								<i>List of valid CampaignIDs. If used, a list of available part payment campaign options will be filtered through the chosen list.</i>
							</td>
							<td class="FormFieldCell">
								<asp:TextBox runat="server" ID="txtActivePartPaymentCampaigns" CssClass="sveawebpaypayment-parameters-url" />
							</td>
						</tr>

						<tr>
							<td class="FormLabelCell">
								<p>Promoted Part Payment Campaign: </p>
								<i>If used, the chosen campaign will be listed first in all payment method lists.</i>
							</td>
							<td class="FormFieldCell">
								<asp:TextBox runat="server" ID="txtPromotedPartPaymentCampaign" CssClass="sveawebpaypayment-parameters-url" />
							</td>
						</tr>

						<tr>
							<td class="FormLabelCell">
								<p>Require electronic ID authentication: </p>
								<i></i>
							</td>
							<td class="FormFieldCell">
								<asp:CheckBox runat="server" ID="chkRequireElectronicIdAuthentication" CssClass="" />
							</td>
						</tr>
					</tbody>
				</table>
			</asp:Panel>
		</div>
	</ContentTemplate>
</asp:UpdatePanel>
