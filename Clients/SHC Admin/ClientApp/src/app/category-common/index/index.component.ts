import { ICategoryCommon } from './../../../shared/Interfaces';
import { AfterViewInit, Component, Injector, OnInit, ViewChild } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { MAT_DIALOG_DATA, MatButton, MatDialog, MatDialogRef, MatSort } from '@angular/material';
import { Subject, merge, of } from 'rxjs';
import { catchError, map, startWith, switchMap } from 'rxjs/operators';
import { compact, isEmpty, omitBy, zipObject } from 'lodash';
import * as _ from 'lodash';
import { DataService } from '@shared/service-proxies/service-data';
import { ILanguage, IBookingTimeslots } from '@shared/Interfaces';
import { PagedListingComponentBase } from '@shared/paged-listing-component-base';
import { TaskComponent } from '../task/task.component';
import swal from 'sweetalert2';

export class EntityDto {
  id: number;
  isActive:number;
}

@Component({
  selector: 'app-index',
  templateUrl: './index.component.html',
  styleUrls: ['./index.component.scss']
})
export class IndexComponent extends PagedListingComponentBase<ICategoryCommon> implements OnInit {

  displayedColumns = [ 'orderNumber', 'code', 'name', 'isActive', 'task'];

  constructor(injector: Injector, private _dataService: DataService, public dialog: MatDialog, private _formBuilder: FormBuilder) { super(injector); }

  ngOnInit() {
    this.api = 'catcommon';
    this.dataService = this._dataService;
    this.dialogComponent = TaskComponent;
    this.frmSearch = this._formBuilder.group({ name: []});
  }
  showErrorDeleteMessage() {
    swal(this.l('ErrorDelete'),this.l('CategoryCommonErrorDeleted',''),'error');
  }

  deleteDialogMessage(obj: EntityDto, key: string, id?: number | string) {
    swal({
        title: this.l('AreYouSure'),
        html: this.l('DeleteWarningMessage', obj[key]),
        type: 'warning',
        showCancelButton: true,
        confirmButtonClass: 'mat-raised-button mat-primary bg-danger',
        cancelButtonClass: 'mat-button',
        confirmButtonText: this.l('YesDelete'),
        cancelButtonText: this.l('Cancel'),
        buttonsStyling: false
    }).then((result) => {
        if (result.value) {
          if(obj.isActive==1){
            this.showErrorDeleteMessage();
          }
          else{
            this.dataService.delete(this.api, obj[id ? id : 'id']).subscribe(() => {
              swal(this.l('SuccessfullyDeleted'), this.l('DeletedInSystem', obj[key]), 'success');
              this.paginator.pageIndex = 0;
              this.paginator._changePageSize(this.paginator.pageSize);
          });
          }
        }
    });
}
}
