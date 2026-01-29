import { ComponentFixture, TestBed } from '@angular/core/testing';

import { NewTransactionV2Component } from './new-transaction-v2.component';

describe('NewTransactionV2Component', () => {
  let component: NewTransactionV2Component;
  let fixture: ComponentFixture<NewTransactionV2Component>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [NewTransactionV2Component]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(NewTransactionV2Component);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
