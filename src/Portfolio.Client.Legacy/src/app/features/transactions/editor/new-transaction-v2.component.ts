import { Component } from '@angular/core';
import { FormBuilder, FormControl, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { TransactionTypeService } from '../../../core/services/transaction-type.service';
import { NewTransaction } from '../../../core/models/new-transaction';
import { TransactionType } from '../../../core/models/transaction-type';
import { MatDialogRef } from '@angular/material/dialog';
import { CommonModule } from '@angular/common';
import { MaterialModule } from '../../../material/material.module';

@Component({
    selector: 'app-new-transaction-v2',
    standalone: true,
    imports: [CommonModule, ReactiveFormsModule, MaterialModule],
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

    form = this.fb.group({
        transactionType: ['', Validators.required],
        fromAssetId: [''],
        amountSpent: [0],
        toAssetId: [''],
        amountReceived: [0],
        fee: [0],
        feeAsset: [''],
        date: [new Date(), Validators.required],
        notes: ['']
    });

    get transactionType() { return this.form.get('transactionType') as FormControl; }

    constructor(
        public transactionTypeService: TransactionTypeService,
        private fb: FormBuilder,
        public dialogRef: MatDialogRef<NewTransactionV2Component>
    ) {}

    onSave() {
        if (this.form.invalid) return;
        
        const val = this.form.value;
        const newTransaction: NewTransaction = {
             date: val.date ? new Date(val.date) : new Date(),
             transactionType: val.transactionType || '',
             fromAssetId: val.fromAssetId || '',
             toAssetId: val.toAssetId || '',
             amountSpent: Number(val.amountSpent) || 0,
             amountReceived: Number(val.amountReceived) || 0,
             fee: Number(val.fee) || 0,
             feeAsset: val.feeAsset || '',
             notes: val.notes || '',
             fromAssetPriceInEur: 0, 
             feeAssetPriceInEur: 0
        };

        this.dialogRef.close(newTransaction);
    }

    onCancel() {
        this.dialogRef.close();
    }
}
