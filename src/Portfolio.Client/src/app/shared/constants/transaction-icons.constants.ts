export const TRANSACTION_ICONS: Record<string, { typeIcon: string; fromIcon: string; toIcon: string }> = {
    SWAP: { typeIcon: 'swap_horiz', fromIcon: 'sell', toIcon: 'shopping_cart' },
    WITHDRAWAL: { typeIcon: 'north_east', fromIcon: '', toIcon: '' },
    DEPOSIT: { typeIcon: 'south_east', fromIcon: '', toIcon: '' },
    REWARD: { typeIcon: 'workspace_premium', fromIcon: '', toIcon: '' },
};

export function getTransactionIcons(typeValue: string): { typeIcon: string; fromIcon: string; toIcon: string } {
    return TRANSACTION_ICONS[typeValue?.toUpperCase()] || { typeIcon: 'swap_horiz', fromIcon: 'swap_horiz', toIcon: 'swap_horiz' };
}
