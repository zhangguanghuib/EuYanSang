import { ExtensionOperationRequestFactoryFunctionType, IOperationContext } from "PosApi/Create/Operations";
import { ClientEntities, ProxyEntities } from "PosApi/Entities";
import ClearSystemDiscountRequest from "./ClearSystemDiscountRequest";
import ClearSystemDiscountResponse from "./ClearSystemDiscountResponse";

let getOperationRequest: ExtensionOperationRequestFactoryFunctionType<ClearSystemDiscountResponse> =

    function (
        context: IOperationContext,
        operationId: number,
        actionParameters: string[],
        correlationId: string
    ): Promise<ClientEntities.ICancelableDataResult<ClearSystemDiscountRequest<ClearSystemDiscountResponse>>> {
        let operationRequest: ClearSystemDiscountRequest<ClearSystemDiscountResponse> = new ClearSystemDiscountRequest<ClearSystemDiscountResponse>(correlationId);
        return Promise.resolve(<ClientEntities.ICancelableDataResult<ClearSystemDiscountRequest<ClearSystemDiscountResponse>>>{
            canceled: false,
            data: operationRequest
        });
    }

export default getOperationRequest;