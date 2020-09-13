namespace Contoso
{
    namespace Commerce.Runtime.ReturnPaymentCheck
    {
        using System;
        using System.Linq;
        using System.Collections.Generic;
        using Microsoft.Dynamics.Commerce.Runtime;
        using Microsoft.Dynamics.Commerce.Runtime.DataModel;
        using Microsoft.Dynamics.Commerce.Runtime.Messages;
        using Microsoft.Dynamics.Commerce.Runtime.Services.Messages;
        using Microsoft.Dynamics.Commerce.Runtime.Framework.Exceptions;

        public class ReturnPaymentCheckServiceRequestTrigger : IRequestTrigger
        {
            public IEnumerable<Type> SupportedRequestTypes
            {
                get
                {
                    return new[] { typeof(CreateSalesOrderServiceRequest) };
                }
            }

            public void OnExecuted(Request request, Response response)
            {

            }

            public void OnExecuting(Request request)
            {
                CreateSalesOrderServiceRequest createSalesOrderServiceRequest = request as CreateSalesOrderServiceRequest;
                SalesTransaction salesTransaction = createSalesOrderServiceRequest.Transaction;
                string returnTransactionId = "";
                string returnStore = "";
                string returnTerminalId = "";
                SalesOrder returnSalesOrder = null;

                Dictionary<string, Dictionary<string, decimal>> payments = new Dictionary<string, Dictionary<string, decimal>>();
                Dictionary<string, Dictionary<string, decimal>> returnPayments = new Dictionary<string, Dictionary<string, decimal>>();

                if (salesTransaction != null && salesTransaction.IsReturnByReceipt)
                {
                    SalesLine salesLine = salesTransaction.SalesLines.First(line => !string.IsNullOrEmpty(line.ReturnTransactionId));
                    // Find the orginal transaction id.
                    returnTransactionId = salesLine.ReturnTransactionId;
                    returnStore = salesLine.ReturnStore;
                    returnTerminalId = salesLine.ReturnTerminalId;

                    if (!String.IsNullOrEmpty(returnTransactionId))
                    {
                        var getSalesOrderDetailsByTransactionIdServiceRequest = new GetSalesOrderDetailsByTransactionIdServiceRequest(returnTransactionId, SearchLocation.All);
                        var getSalesOrderDetailsServiceResponse = request.RequestContext.Execute<GetSalesOrderDetailsServiceResponse>(getSalesOrderDetailsByTransactionIdServiceRequest);
                        returnSalesOrder = getSalesOrderDetailsServiceResponse.SalesOrder;
                    }

                    if (!string.IsNullOrEmpty(returnStore) && !string.IsNullOrEmpty(salesTransaction.StoreId) && salesTransaction.StoreId != returnStore)
                    {
                        throw new CommerceException("Microsoft_Dynamics_Commerce_ReturnInDifferentStore", ExceptionSeverity.Warning, null, "Custom error")
                        {
                            LocalizedMessage = "Return in different store from original is not allowed.",
                            LocalizedMessageParameters = new object[] { }
                        };
                    }

                    if (returnSalesOrder != null && salesTransaction != null)
                    {
                        buildPayments(salesTransaction, ref payments);
                        buildPayments(returnSalesOrder, ref returnPayments);
                    }

                    if (!comparePaymentsWithReturnPayments(payments, returnPayments))
                    {
                        throw new CommerceException("Microsoft_Dynamics_Commerce_ReturnPaymentInconsistent", ExceptionSeverity.Warning, null, "Custom error")
                        {
                            LocalizedMessage = "The payment methods or amounts of return are inconsistent with original.",
                            LocalizedMessageParameters = new object[] { }
                        };
                    }
                }
            }

            // Build the payment info of return transaction and original transaction, store it into dictionary.
            private void buildPayments(SalesTransaction salesTransaction, ref Dictionary<string, Dictionary<string, decimal>> payments)
            {
                foreach (TenderLine tenderLine in salesTransaction.TenderLines)
                {
                    if (tenderLine.IsVoided == false)
                    {
                        tenderLine.CardOrAccount = !string.IsNullOrEmpty(tenderLine.CardOrAccount) ? tenderLine.CardOrAccount : string.Empty;
                        if (!payments.ContainsKey(tenderLine.TenderTypeId))
                        {
                            Dictionary<string, decimal> entry = new Dictionary<string, decimal>();
                            entry.Add(tenderLine.CardOrAccount, tenderLine.Amount);
                            payments.Add(tenderLine.TenderTypeId, entry);
                        }
                        else
                        {
                            Dictionary<string, decimal> entry = payments[tenderLine.TenderTypeId];
                            if (!entry.ContainsKey(tenderLine.CardOrAccount))
                            {
                                entry.Add(tenderLine.CardOrAccount, tenderLine.Amount);
                            }
                            else
                            {
                                entry[tenderLine.CardOrAccount] += tenderLine.Amount;
                            }
                        }
                    }
                }
            }

            // Compare the return transaction payment info and the orignal payment info.
            private bool comparePaymentsWithReturnPayments(Dictionary<string, Dictionary<string, decimal>> payments,
                Dictionary<string, Dictionary<string, decimal>> returnPayments)
            {
                bool ret = true;

                if(payments.Keys.Count != returnPayments.Keys.Count)
                {
                    ret = false;
                }

                if(ret)
                {
                    foreach(string tenderTypeId in payments.Keys)
                    {
                        if(!returnPayments.ContainsKey(tenderTypeId))
                        {
                            ret = false;
                            break;
                        }
                        Dictionary<string, decimal> payment = payments[tenderTypeId];
                        Dictionary<string, decimal> retPayment = returnPayments[tenderTypeId];

                        if (payment.Keys.Count != retPayment.Keys.Count)
                        {
                            ret = false;
                            break;
                        }
                        else
                        {
                            foreach(string cardAccount in payment.Keys)
                            {
                                if(!retPayment.ContainsKey(cardAccount) || Math.Abs(retPayment[cardAccount]) != Math.Abs(payment[cardAccount]))
                                {
                                    ret = false;
                                    break;
                                }
                            }
                        }
                    }
                }

                return ret;
            }
        }
    }
}
