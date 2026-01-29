import Decimal from "decimal.js";

export type Inventory = {
    [asset: string]: {
        quantity: Decimal;
        costInEur: Decimal;
    }[];
};