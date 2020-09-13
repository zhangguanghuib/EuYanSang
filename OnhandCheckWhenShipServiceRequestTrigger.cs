namespace Contoso
{
    namespace Commerce.Runtime.ShippingOnhandCheck
    {
        using System;
        using System.Collections.Generic;
        using System.Text;
        using Microsoft.Dynamics.Commerce.Runtime;
        using Microsoft.Dynamics.Commerce.Runtime.DataModel;
        using Microsoft.Dynamics.Commerce.Runtime.DataServices.Messages;
        using Microsoft.Dynamics.Commerce.Runtime.Messages;
        using Microsoft.Dynamics.Commerce.Runtime.Services;
        using Microsoft.Dynamics.Commerce.Runtime.Services.Messages;
        using Microsoft.Dynamics.Commerce.Runtime.Workflow;
        using Microsoft.Dynamics.Commerce.Runtime.Client;
        using System.Linq;
        using System.Runtime.InteropServices;

        public class OnhandCheckWhenShipServiceRequestTrigger : IRequestTrigger
        {
            public IEnumerable<Type> SupportedRequestTypes
            {
                get
                {
                    return new[] { typeof(SaveCartRequest) };
                }
            }

            public void OnExecuted(Request request, Response response)
            {
               
            }

            public void OnExecuting(Request request)
            {
                SaveCartRequest saveCartRequest = request as SaveCartRequest;
                SalesTransaction salesTransaction = saveCartRequest.SalesTransaction;
                Dictionary<string, bool> checkDict = new Dictionary<string, bool>();

                if (saveCartRequest.Cart.CartType == CartType.CustomerOrder && saveCartRequest.Cart.CustomerOrderMode == CustomerOrderMode.CustomerOrderCreateOrEdit)
                {
                    if (salesTransaction == null)
                    {
                        salesTransaction = CartWorkflowHelper.LoadSalesTransaction(saveCartRequest.RequestContext, saveCartRequest.Cart.Id, saveCartRequest.Cart.Version);
                    }

                    if (salesTransaction != null 
                        && salesTransaction.CartType == CartType.CustomerOrder && salesTransaction.CustomerOrderMode == CustomerOrderMode.CustomerOrderCreateOrEdit
                        && (!String.IsNullOrEmpty(saveCartRequest.Cart.DeliveryMode)))
                    {

                        bool DeliveryOnhandCheck = saveCartRequest.Cart.DeliveryMode == "99" ? true : false ;

                        if (DeliveryOnhandCheck == true)
                        {
                            InventoryManager inventoryManager = InventoryManager.Create(request.RequestContext.Runtime);
                            foreach (SalesLine salesLine in salesTransaction.InventorySalesLines)
                            {
                                PagingInfo pagingInfo = new PagingInfo(30, 0);
                                SortingInfo sortingInfo = new SortingInfo();
                                var settings = new QueryResultSettings(pagingInfo, sortingInfo);

                                PagedResult<OrgUnitAvailability> orgUnitAvailabilities = inventoryManager.SearchAvailableInventory(salesLine.ProductId, null, settings);
                                foreach (OrgUnitAvailability orgUnitAvailability in orgUnitAvailabilities)
                                {
                                    if(checkDict.ContainsKey(salesLine.LineId))
                                    {
                                        break;
                                    }
                  
                                    foreach (ItemAvailability itemAvailability in orgUnitAvailability.ItemAvailabilities)
                                    {
                                        if (itemAvailability.ProductId == salesLine.ProductId
                                            && (itemAvailability.InventoryLocationId == salesLine.InventoryLocationId || string.IsNullOrEmpty(salesLine.InventoryLocationId))
                                            //  && itemAvailability.VariantInventoryDimensionId == salesLine.InventoryDimensionId
                                            && itemAvailability.AvailableQuantity >= salesLine.Quantity)
                                        {
                                            checkDict.Add(salesLine.LineId, true);
                                            break;
                                        }
                                    }
                                }

                                if (!checkDict.ContainsKey(salesLine.LineId))
                                {
                                    checkDict.Add(salesLine.LineId, false);
                                }
                            }

                            if (checkDict.ContainsValue(false))
                            { 
                                throw new CartValidationException(DataValidationErrors.Microsoft_Dynamics_Commerce_Runtime_InvalidDeliveryMode, String.Format("Delivery mode {0} is not applicable since some product are out stock", salesTransaction.DeliveryMode));
                            }
                        }

                    }
                }
            }
        }
    }
}
