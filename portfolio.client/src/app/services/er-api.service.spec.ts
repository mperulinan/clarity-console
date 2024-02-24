import { TestBed } from '@angular/core/testing';

import { ErApiService } from './er-api.service';

describe('ErApiService', () => {
  let service: ErApiService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(ErApiService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
