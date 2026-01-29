import { Component } from '@angular/core';
import { FormBuilder, FormControl, FormGroup, Validators } from '@angular/forms';
import { TransactionTypeService } from '../services/transaction-type.service';
import { BaseTransaction, NewTransaction, RewardTransaction, SwapTransaction, TransferTransaction } from '../models/new-transaction';
import { TransactionType } from '../models/transaction-type';

@Component({
    selector: 'app-new-transaction-v2',
    templateUrl: './new-transaction-v2.component.html',
    styleUrl: './new-transaction-v2.component.scss'
})
export class NewTransactionV2Component {
    public TYPE_SWAP = this.transactionTypeService.TYPE_SWAP;
    public TYPE_REWARD = this.transactionTypeService.TYPE_REWARD;
    public TYPE_TRANSFER = this.transactionTypeService.TYPE_TRANSFER;
    public transactionTypeOptions = this.transactionTypeService.options;

    get selectedType() {
        return this.transactionType.value;
    }

    step = 1;

    transactionType: FormControl<string | null> = new FormControl(this.TYPE_SWAP, Validators.required);
    date: FormControl<Date | null> = new FormControl(null, Validators.required);
    typeGroup: FormGroup = new FormGroup({
        type: this.transactionType,
        date: this.date
    });

    detailsGroup = new FormGroup({});

    fee: FormControl<number | null> = new FormControl(null);
    feeAsset: FormControl<string | null> = new FormControl(null);
    feeAssetPriceInEur: FormControl<number | null> = new FormControl(null);
    feeGroup: FormGroup = new FormGroup({
        fee: this.fee,
        feeAsset: this.feeAsset,
        feeAssetPriceEur: this.feeAssetPriceInEur
    });

    notes: FormControl<string | null> = new FormControl('');
    notesGroup: FormGroup = new FormGroup({
        notes: this.notes
    });
    form = new FormGroup({
        typeGroup: this.typeGroup,
        detailsGroup: new FormGroup({}),
        feeGroup: this.feeGroup,
        notesGroup: this.notesGroup
    });

    private detailsBuilders: { [key: string]: () => FormGroup } = {};

    constructor(
        private transactionTypeService: TransactionTypeService,
        private fb: FormBuilder,
    ) { }

    ngOnInit(): void {
        this.createGroupBuilders();
        this.onTransactionTypeChange(this.TYPE_SWAP);

        this.transactionType.valueChanges.subscribe(type => {
            if (!type) {
                return;
            }
            this.onTransactionTypeChange(type);
        });
    }

    private createGroupBuilders() {
        this.detailsBuilders[this.TYPE_SWAP] = () => this.fb.group({
            fromAssetId: this.fb.control('', Validators.required),
            toAssetId: this.fb.control('', Validators.required),
            amountSpent: this.fb.control('', Validators.required),
            amountReceived: this.fb.control('', Validators.required),
            fromAssetPriceInEur: this.fb.control('', Validators.required),
        });

        this.detailsBuilders[this.TYPE_REWARD] = () => this.fb.group({
            rewardAsset: this.fb.control('', Validators.required),
            rewardAmount: this.fb.control('', Validators.required),
            rewardPriceEur: this.fb.control('', Validators.required),
        });

        this.detailsBuilders[this.TYPE_TRANSFER] = () => this.fb.group({
            transferAmount: this.fb.control('', Validators.required),
        });
    }

    onTransactionTypeChange(type: string): void {
        const builder = this.detailsBuilders[type];
        if (builder) {
            this.detailsGroup = builder();
            this.form.setControl('detailsGroup', this.detailsGroup);
        }
        console.log("onTransactionTypeChange", this.form);
    }

    nextStep() {
        if (this.step === 1) {
            if (this.typeGroup.invalid || this.detailsGroup.invalid) {
                return; // Don't advance if invalid
            }
            if (this.selectedType === this.TYPE_TRANSFER) {
                this.step = 3; // Skip to Notes for TRANSFER
            } else {
                this.step = 2; // Go to Fees for others
            }
        } else {
            this.step++;
        }
    }

    prevStep() {
        if (this.step === 3) {
            if (this.selectedType === this.TYPE_TRANSFER) {
                this.step = 1; // Back to Step 1 for TRANSFER (skipped 2)
            } else {
                this.step = 2;
            }
        } else {
            this.step--;
        }
    }

    private mapToTransactionType(code: string): TransactionType {  // Usa el tipo real
        const option = this.transactionTypeOptions.find(opt => opt.value === code);
        return {
            code,
            name: option?.label || code // Fallback
        };
    }

    // Generic helper
    private extractDetails<T extends NewTransaction>(code: string): Omit<T, keyof BaseTransaction> | null {
        if (!this.detailsBuilders[code]) {
            console.error('Builder not found for type:', code);
            return null;
        }

        const formValue = this.detailsGroup.value as Partial<Omit<T, keyof BaseTransaction>>;

        const details: Omit<T, keyof BaseTransaction> = {} as Omit<T, keyof BaseTransaction>;
        Object.keys(formValue).forEach(key => {
            const typedKey = key as keyof Omit<T, keyof BaseTransaction>;
            const val = formValue[typedKey];
            (details as any)[typedKey] = val?.toString() ?? "0";  // Default "0"
        });

        // Check for required fields
        const requiredKeys = Object.keys(details) as (keyof Omit<T, keyof BaseTransaction>)[];
        for (const key of requiredKeys) {
            if (details[key] === undefined || details[key] === '') {
                console.error(`Missing required field in ${code}: ${String(key)}`);
                return null;
            }
        }

        return details;
    }

    submit() {
        if (this.form.invalid) {
            console.error('Invalid form.');
            return;
        }

        const typeCode = this.transactionType.value;
        const date = this.date.value;

        if (!typeCode || !date) {
            console.error('Missing required fields.');
            return null;
        }

        if (!this.transactionTypeOptions.find(opt => opt.value === typeCode)) {
            console.error('Invalid transaction type:', typeCode);
            return null;
        }

        const base: BaseTransaction = {
            date,
            transactionType: this.mapToTransactionType(typeCode),
            notes: this.notes.value ?? null,
            fee: this.fee.value?.toString() ?? "0",
            feeAsset: this.feeAsset.value ?? null,
            feeAssetPriceInEur: this.feeAssetPriceInEur.value?.toString() ?? null,
        };

        let transactionData: NewTransaction | null = null;
        switch (typeCode) {
            case this.TYPE_SWAP: {
                const swapDetails = this.extractDetails<SwapTransaction>(typeCode);
                if (!swapDetails) {
                    return null;
                }
                transactionData = {
                    ...base,
                    ...swapDetails
                } as SwapTransaction;
                break;
            }
            case this.TYPE_REWARD: {
                const rewardDetails = this.extractDetails<RewardTransaction>(typeCode);
                if (!rewardDetails) {
                    return null;
                }
                transactionData = {
                    ...base,
                    ...rewardDetails
                } as RewardTransaction;
                break;
            }
            case this.TYPE_TRANSFER: {
                const transferDetails = this.extractDetails<TransferTransaction>(typeCode);
                if (!transferDetails) {
                    return null;
                }
                transactionData = {
                    ...base,
                    ...transferDetails
                } as TransferTransaction;
                break;
            }
            default:
                console.warn('Not handled transaction type:', typeCode);
                return null;
        }

        console.log('Transacción a guardar:', transactionData);

        // Lógica de guardado
        // this.transactionService.save(transactionData).subscribe(...);

        return transactionData;
    }
}
