import * as Triggers from "PosApi/Extend/Triggers/PeripheralTriggers"
import { ObjectExtensions } from "PosApi/TypeExtensions";
import { ClientEntities, ProxyEntities } from "PosApi/Entities";

export default class PostOpenCashDrawerTrigger extends Triggers.PostOpenCashDrawerTrigger {

    public execute(options: Triggers.IPostOpenCashDrawerTriggerOptions): Promise<void> {
        return Promise.resolve();
    }
}