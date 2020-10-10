namespace Contoso
{
    namespace Commerce.Runtime.ReceiptsSample
    {
        using System;
        using System.Collections.Generic;
        using Microsoft.Dynamics.Commerce.Runtime;
        using Microsoft.Dynamics.Commerce.Runtime.Messages;
        using Microsoft.Dynamics.Commerce.Runtime.DataServices.Messages;
        using Microsoft.Dynamics.Commerce.Runtime.RealtimeServices.Messages;
        using System.Collections.ObjectModel;
        using Microsoft.Dynamics.Commerce.Runtime.DataModel;

        public class LoyaltyServiceRequestTrigger : IRequestTrigger
        {
            public IEnumerable<Type> SupportedRequestTypes
            {
                get
                {
                    return new[] { typeof(SaveCartVersionedDataRequest) };
                }
            }

            public void OnExecuted(Request request, Response response)
            {
            }

            public void OnExecuting(Request request)
            {
                if (request is SaveCartVersionedDataRequest)
                {
                    SaveCartVersionedDataRequest saveCartVersionedDataRequest = request as SaveCartVersionedDataRequest;

                    DeviceConfiguration deviceConfiguration = request.RequestContext.GetDeviceConfiguration();

                    if (request.RequestContext.GetDeviceConfiguration().EnableAxCustomerSearch == true
                        && !string.IsNullOrEmpty(saveCartVersionedDataRequest.SalesTransaction.CustomerId) 
                        && string.IsNullOrEmpty(saveCartVersionedDataRequest.SalesTransaction.LoyaltyCardId))
                    {
                        InvokeExtensionMethodRealtimeRequest extensionRequest = new InvokeExtensionMethodRealtimeRequest("GetCustomerLoyaltyCard",
                            saveCartVersionedDataRequest.SalesTransaction.CustomerId,
                            request.RequestContext.GetChannelConfiguration().InventLocationDataAreaId);

                        InvokeExtensionMethodRealtimeResponse extensionResponse = request.RequestContext.Execute<InvokeExtensionMethodRealtimeResponse>(extensionRequest);
                        ReadOnlyCollection<object> results = extensionResponse.Result;

                        string loyalyCardNumber = (string)results[0];
                        saveCartVersionedDataRequest.SalesTransaction.LoyaltyCardId = loyalyCardNumber;
                    }
                }
            }
        }
    }
}