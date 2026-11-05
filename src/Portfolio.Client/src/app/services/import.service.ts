import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  BrokerDto,
  CommitImportTransactionsRequest,
  ImportTransactionsPreviewDto,
  ImportTransactionsResultDto
} from '../models/import';

@Injectable({
  providedIn: 'root'
})
export class ImportService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  listBrokers(): Observable<BrokerDto[]> {
    return this.http.get<BrokerDto[]>(`${this.apiUrl}/Import/brokers`);
  }

  preview(brokerKey: string, file: File): Observable<ImportTransactionsPreviewDto> {
    const formData = new FormData();
    formData.append('file', file, file.name);
    return this.http.post<ImportTransactionsPreviewDto>(
      `${this.apiUrl}/Import/${brokerKey}/preview`,
      formData
    );
  }

  commit(
    brokerKey: string,
    request: CommitImportTransactionsRequest
  ): Observable<ImportTransactionsResultDto> {
    return this.http.post<ImportTransactionsResultDto>(
      `${this.apiUrl}/Import/${brokerKey}/commit`,
      request
    );
  }
}

