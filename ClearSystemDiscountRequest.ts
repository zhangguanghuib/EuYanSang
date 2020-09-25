import { ExtensionOperationRequestBase } from "PosApi/Create/Operations";

import ClearSystemDiscountResponse from "./ClearSystemDiscountResponse";

export default class ClearSystemDiscountRequest<TResponse extends ClearSystemDiscountResponse> extends ExtensionOperationRequestBase<TResponse>{
    constructor(correlationId: string) {
        super(5005, correlationId);
    }
}