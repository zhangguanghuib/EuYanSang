import { ExtensionOperationRequestHandlerBase, ExtensionOperationRequestType } from "PosApi/Create/Operations";
import { ClientEntities, ProxyEntities } from "PosApi/Entities";
import ClearSystemDiscountRequest from "./ClearSystemDiscountRequest";
import ClearSystemDiscountResponse from "./ClearSystemDiscountResponse";
import {
    GetCurrentCartClientRequest,
    GetCurrentCartClientResponse,
    LineDiscountAmountOperationRequest,
    LineDiscountAmountOperationResponse
} from "PosApi/Consume/Cart";

import { ArrayExtensions, ObjectExtensions, StringExtensions } from "PosApi/TypeExtensions";
import MessageHelpers from "../../Utilities/MessageHelpers";
import CartViewController from "../../ViewExtensions/Cart/CartViewController";

import { IExtensionContext } from "PosApi/Framework/ExtensionContext";
import {
    IListInputDialogItem, IListInputDialogOptions,
    ShowListInputDialogClientRequest, ShowListInputDialogClientResponse
} from "PosApi/Consume/Dialogs";

export default class ClearSytemDiscountRequestHandler<TReponse extends ClearSystemDiscountResponse> extends ExtensionOperationRequestHandlerBase<TReponse>{

    public supportedRequestType(): ExtensionOperationRequestType<TReponse> {
        return ClearSystemDiscountRequest;
    }

    public executeAsync(request: ClearSystemDiscountRequest<TReponse>): Promise<ClientEntities.ICancelableDataResult<TReponse>> {

        const discountAmount: number = 0.00001;

        this.context.logger.logInformational("PromotionsViewModel: Setting line discount. Discount percentage: " + discountAmount.toString());

        let cart: ProxyEntities.Cart = null;
        let cartRequest: GetCurrentCartClientRequest<GetCurrentCartClientResponse> = new GetCurrentCartClientRequest();

        return this.context.runtime.executeAsync(cartRequest)
            .then((result: ClientEntities.ICancelableDataResult<GetCurrentCartClientResponse>): void => {
                if (!result.canceled) {
                    cart = result.data.result;
                }
            }).then((): Promise<ClientEntities.ICancelableDataResult<LineDiscountAmountOperationResponse>> => {
                // If the cart is not defined then the operation is cancelled
                if (ObjectExtensions.isNullOrUndefined(cart)) {
                    //  this._customViewControllerBaseState.isProcessing = false;
                    let noopResponse: ClientEntities.ICancelableDataResult<LineDiscountAmountOperationResponse> = {
                        canceled: true,
                        data: null
                    };

                    return Promise.resolve(noopResponse);
                }

                let selectedCartLineId: string = CartViewController.selectedCartLineId;
                if (StringExtensions.isNullOrWhitespace(selectedCartLineId)) {
                    return this._showDialog(this.context, cart)
                        .then((dialogResult: ClientEntities.ICancelableDataResult<string>) => {
                            return Promise.resolve(<ClientEntities.ICancelableDataResult<LineDiscountAmountOperationResponse>>{ canceled: true, data: null });
                        });
                } 
                let cartLineDiscounts: ClientEntities.ICartLineDiscount[] = [];
                cart.CartLines.forEach((line: ProxyEntities.CartLine) => {
                    if (line.LineId === selectedCartLineId) {
                        cartLineDiscounts.push({ cartLine: line, discountValue: discountAmount });
                    }
                })
                        
                // Set the total discount on the cart
                let request: LineDiscountAmountOperationRequest<LineDiscountAmountOperationResponse> =
                    new LineDiscountAmountOperationRequest<LineDiscountAmountOperationResponse>(
                        cartLineDiscounts,
                        this.context.logger.getNewCorrelationId()
                    );
                return this.context.runtime.executeAsync(request);
            }).then((result: ClientEntities.ICancelableDataResult<LineDiscountAmountOperationResponse>):
                ClientEntities.ICancelableDataResult<ClearSystemDiscountResponse> => {

                return <ClientEntities.ICancelableDataResult<ClearSystemDiscountResponse>>{
                    canceled: result.canceled,
                    data: result.canceled ? null : new ClearSystemDiscountResponse()
                };
            }).catch((reason: any) => {
                return MessageHelpers.ShowErrorMessage(
                    this.context,
                    JSON.stringify(reason),
                    reason
                );
            });
    }

    private _showDialog(context: IExtensionContext, cart: ProxyEntities.Cart): Promise<ClientEntities.ICancelableDataResult<string>> {

        let convertedListItems: IListInputDialogItem[] = cart.CartLines.map((cartLine: ProxyEntities.CartLine): IListInputDialogItem => {
            return {
                label: cartLine.Description, // string to be displayed for the given item
                value: cartLine.LineId // list data item that the string label represents
            };
        });

        let listInputDialogOptions: IListInputDialogOptions = {
            title: "Select cart line",
            subTitle: "Cart lines",
            items: convertedListItems
        };

        let dialogRequest: ShowListInputDialogClientRequest<ShowListInputDialogClientResponse> =
            new ShowListInputDialogClientRequest<ShowListInputDialogClientResponse>(listInputDialogOptions);

        return context.runtime.executeAsync(dialogRequest)
            .then((result: ClientEntities.ICancelableDataResult<ShowListInputDialogClientResponse>) => {
                if (result.canceled) {
                    return Promise.resolve(<ClientEntities.ICancelableDataResult<string>>{ canceled: true, data: StringExtensions.EMPTY });
                }

                return Promise.resolve(<ClientEntities.ICancelableDataResult<string>>{ canceled: false, data: result.data.result.value.value });
            });
    }

}