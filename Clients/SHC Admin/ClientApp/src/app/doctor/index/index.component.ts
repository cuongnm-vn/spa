import { IHealthfacilities } from './../../../../../../SHC Outside/ClientApp/src/shared/Interfaces';
import { IDoctor } from './../../../../../../SHC Client/ClientApp/src/shared/Interfaces';
import { ICategoryCommon } from './../../../shared/Interfaces';
import { AfterViewInit, Component, Injector, OnInit, ViewChild, Input } from '@angular/core';
import { FormBuilder, FormGroup, FormControl } from '@angular/forms';
import { MAT_DIALOG_DATA, MatButton, MatDialog, MatDialogRef, MatSort, MatCheckbox, MatInput, AUTOCOMPLETE_OPTION_HEIGHT, MatSelect } from '@angular/material';
import { Subject, merge, of, Observable } from 'rxjs';
import { catchError, map, startWith, switchMap } from 'rxjs/operators';
import { compact, isEmpty, omitBy, zipObject } from 'lodash';
import * as _ from 'lodash';
import { DataService } from '@shared/service-proxies/service-data';
import { ILanguage, IBookingTimeslots } from '@shared/Interfaces';
import { PagedListingComponentBase } from '@shared/paged-listing-component-base';
import { TaskComponent } from '../task/task.component';
import swal from 'sweetalert2';
import { start } from 'repl';



export class DoctorEntityDto {
  doctorId: number;
}

export class EntityDto {
  id: number;
}

@Component({
  selector: 'app-index',
  templateUrl: './index.component.html',
  styleUrls: ['./index.component.scss']
})
export class IndexComponent extends PagedListingComponentBase<ICategoryCommon> implements OnInit {

  provinces = [];
  districts = [];
  checkProvince = false;
  _healthfacilities = [];
  specialist = [];


  filteredHealthFacilitiesOptions: Observable<IHealthfacilities[]>;

  healthfacilities = new FormControl();

  displayedColumns = ['orderNumber', 'fullName', 'specialistName', 'phoneNumber', 'address', 'priceFrom', 'allowBooking', 'allowFilter', 'allowSearch', 'task'];

  constructor(injector: Injector, private _dataService: DataService, public dialog: MatDialog, private _formBuilder: FormBuilder) { super(injector); }


  ngOnInit() {
    this.api = 'doctor';
    this.dataService = this._dataService;
    this.dialogComponent = TaskComponent;
    this.frmSearch = this._formBuilder.group({ provinceCode: [], districtCode: [], info: [], healthfacilities: [], specialistCode: [] });
    this.getProvinces();
    this.getSpecialist();

    this.dataService.getAll("healthfacilities",String(this.appSession.user.healthFacilitiesId))
      .subscribe(resp => this._healthfacilities = resp.items);
    if (this.appSession.user.healthFacilitiesId) {
      this.dataService.getAll("healthfacilities",String(this.appSession.user.healthFacilitiesId))
        .subscribe(resp => this.dataSources = resp.items);
    }
    this.appSession.user.healthFacilitiesId ? this.frmSearch.controls['healthfacilities']
      .setValue(this.appSession.user.healthFacilitiesId) : this.filterOptions();

  }
  showErrorDeleteMessage() {
    swal(this.l('ErrorDelete'), this.l('DoctorErrorDeleted', ''), 'error');
  }

  deleteDialogDoctor(obj: DoctorEntityDto, key: string, doctorId?: number | string) {
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
        this.dataService.delete(this.api, obj[doctorId ? doctorId : 'doctorId']).subscribe(() => {
          swal(this.l('SuccessfullyDeleted'), this.l('DeletedInSystem', obj[key]), 'success');
          this.paginator.pageIndex = 0;
          this.paginator._changePageSize(this.paginator.pageSize);
        });
      }
    })
  }

  getProvinces() {
    this._dataService.getAll("provinces").subscribe(resp => this.provinces = resp.items);
  }

  getDistricts(provinceCode) {
    this._dataService.getAll("districts", "{ProvinceCode:" + provinceCode + "}").subscribe(resp => this.districts = resp.items);
  }

  provinceChange($event) {
    this.frmSearch.patchValue({ districtCode: null });
    if ($event.value != undefined) {
      this.getDistricts($event.value);
      this.checkProvince = true;
      this.getHealthfacilities($event.value);
    }
    else {
      this.checkProvince = false;
      this.districts = null;
      if (!this.appSession.user.healthFacilitiesId)
        this._healthfacilities = null;
    }
  }

  districtChange(province, $event) {
    this.getHealthfacilities(province.value, $event.value);
  }

  resetSearch() {
    this.provinces = null;
    this.specialist = null;
    this.specialist = null;
    if (!this.appSession.user.healthFacilitiesId) {
      this._healthfacilities = null;
    }
  }

  doctorUpdateAllowBooking(obj?: IDoctor): void {
    if (obj.allowBooking == false) {
      swal({
        title: this.l('AreYouSure'),
        html: this.l('DoctorTitle') + ' ' + obj.fullName + ' ' + this.ll('DoctorUpdateAllowBookingWarningMessage'),
        type: 'warning',
        showCancelButton: true,
        confirmButtonClass: 'mat-raised-button mat-primary bg-danger',
        cancelButtonClass: 'mat-button',
        confirmButtonText: this.l('YesUpdate'),
        cancelButtonText: this.l('Cancel'),
        buttonsStyling: false
      }).then((result) => {
        if (result.value) {
          obj.allowBooking = !obj.allowBooking;
          obj.updateUserId = this.appSession.userId;
          this.dataService.update(this.api + "?allow=1", obj).subscribe(() => {
            swal(this.l('DoctorUpdateAllowCompleteTitle'), this.l('DoctorTitle') + ' ' + obj.fullName + ' ' + this.ll('DoctorUpdateAllowBookingSuccessfully', obj.fullName), 'success');
            this.paginator.pageIndex = 0;
            this.paginator._changePageSize(this.paginator.pageSize);
          });
          this.resetSearch();
          this.ngOnInit();
        }
      });
    }
    else {
      swal({
        title: this.l('AreYouSure'),
        html: this.l('DoctorTitle') + ' ' + obj.fullName + ' ' + this.ll('DoctorCancelAllowBookingWarningMessage'),
        type: 'warning',
        showCancelButton: true,
        confirmButtonClass: 'mat-raised-button mat-primary bg-danger',
        cancelButtonClass: 'mat-button',
        confirmButtonText: this.l('YesUpdate'),
        cancelButtonText: this.l('Cancel'),
        buttonsStyling: false
      }).then((result) => {
        if (result.value) {
          obj.allowBooking = !obj.allowBooking;
          obj.updateUserId = this.appSession.userId;
          this.dataService.update(this.api + "?allow=1", obj).subscribe(() => {
            swal(this.l('DoctorUpdateAllowCompleteTitle'), this.l('DoctorTitle') + ' ' + obj.fullName + ' ' + this.ll('DoctorCancelAllowBookingSuccessfully', obj.fullName), 'success');
            this.paginator.pageIndex = 0;
            this.paginator._changePageSize(this.paginator.pageSize);
          });
          this.resetSearch();
          this.ngOnInit();
        }
      });
    }
  }

  doctorUpdateAllowFilter(obj?: IDoctor): void {
    if (obj.allowFilter == false) {
      swal({
        title: this.l('AreYouSure'),
        html: this.l('DoctorTitle') + ' ' + obj.fullName + ' ' + this.ll('DoctorUpdateAllowFilterWarningMessage'),
        type: 'warning',
        showCancelButton: true,
        confirmButtonClass: 'mat-raised-button mat-primary bg-danger',
        cancelButtonClass: 'mat-button',
        confirmButtonText: this.l('YesUpdate'),
        cancelButtonText: this.l('Cancel'),
        buttonsStyling: false
      }).then((result) => {
        if (result.value) {
          obj.allowFilter = !obj.allowFilter;
          obj.updateUserId = this.appSession.userId;
          this.dataService.update(this.api + "?allow=1", obj).subscribe(() => {
            swal(this.l('DoctorUpdateAllowCompleteTitle'), this.l('DoctorTitle') + ' ' + obj.fullName + ' ' + this.ll('DoctorUpdateAllowFilterSuccessfully', obj.fullName), 'success');
            this.paginator.pageIndex = 0;
            this.paginator._changePageSize(this.paginator.pageSize);
          });
          this.resetSearch();
          this.ngOnInit();
        }
      });
    }
    else {
      swal({
        title: this.l('AreYouSure'),
        html: this.l('DoctorTitle') + ' ' + obj.fullName + ' ' + this.ll('DoctorCancelAllowFilterWarningMessage'),
        type: 'warning',
        showCancelButton: true,
        confirmButtonClass: 'mat-raised-button mat-primary bg-danger',
        cancelButtonClass: 'mat-button',
        confirmButtonText: this.l('YesUpdate'),
        cancelButtonText: this.l('Cancel'),
        buttonsStyling: false
      }).then((result) => {
        if (result.value) {
          obj.allowFilter = !obj.allowFilter;
          obj.updateUserId = this.appSession.userId;
          this.dataService.update(this.api + "?allow=1", obj).subscribe(() => {
            swal(this.l('DoctorUpdateAllowCompleteTitle'), this.l('DoctorTitle') + ' ' + obj.fullName + ' ' + this.ll('DoctorCancelAllowFilterSuccessfully', obj.fullName), 'success');
            this.paginator.pageIndex = 0;
            this.paginator._changePageSize(this.paginator.pageSize);
          });
          this.resetSearch();
          this.ngOnInit();
        }
      });
    }
  }

  doctorUpdateAllowSearch(obj?: IDoctor): void {
    if (obj.allowSearch == false) {
      swal({
        title: this.l('AreYouSure'),
        html: this.l('DoctorTitle') + ' ' + obj.fullName + ' ' + this.l('DoctorUpdateAllowSearchWarningMessage'),
        type: 'warning',
        showCancelButton: true,
        confirmButtonClass: 'mat-raised-button mat-primary bg-danger',
        cancelButtonClass: 'mat-button',
        confirmButtonText: this.l('YesUpdate'),
        cancelButtonText: this.l('Cancel'),
        buttonsStyling: false
      }).then((result) => {
        if (result.value) {
          obj.allowSearch = !obj.allowSearch;
          obj.updateUserId = this.appSession.userId;
          this.dataService.update(this.api + "?allow=1", obj).subscribe(() => {
            swal(this.l('DoctorUpdateAllowCompleteTitle'), this.l('DoctorTitle') + ' ' + obj.fullName + ' ' + this.l('DoctorUpdateAllowSearchSuccessfully', obj.fullName), 'success');
            this.paginator.pageIndex = 0;
            this.paginator._changePageSize(this.paginator.pageSize);
          });
          this.resetSearch();
          this.ngOnInit();
        }
      });
    }
    else {
      swal({
        title: this.l('AreYouSure'),
        html: this.l('DoctorTitle') + ' ' + obj.fullName + ' ' + this.l('DoctorCancelAllowSearchWarningMessage'),
        type: 'warning',
        showCancelButton: true,
        confirmButtonClass: 'mat-raised-button mat-primary bg-danger',
        cancelButtonClass: 'mat-button',
        confirmButtonText: this.l('YesUpdate'),
        cancelButtonText: this.l('Cancel'),
        buttonsStyling: false
      }).then((result) => {
        if (result.value) {
          obj.allowSearch = !obj.allowSearch;
          obj.updateUserId = this.appSession.userId;
          this.dataService.update(this.api + "?allow=1", obj).subscribe(() => {
            swal(this.l('DoctorUpdateAllowCompleteTitle'), this.l('DoctorTitle') + ' ' + obj.fullName + ' ' + this.l('DoctorCancelAllowSearchSuccessfully', obj.fullName), 'success');
            this.paginator.pageIndex = 0;
            this.paginator._changePageSize(this.paginator.pageSize);
          });
          this.resetSearch();
          this.ngOnInit();
        }
      });
    }
  }

  getHealthfacilities(provinceCode, districtCode?) {
    if (!this.appSession.user.healthFacilitiesId) {
      if (districtCode == null) {
        this.dataService.getAll("healthfacilities","{ provinceCode:" + provinceCode + "}")
          .subscribe(resp => this._healthfacilities = resp.items);
      }
      else {
        console.log("aa");
        this.dataService.getAll("healthfacilities","{provinceCode:" + provinceCode + ",districtCode:" + districtCode + "}")
          .subscribe(resp => this._healthfacilities = resp.items)
      }
    }
  }


  getSpecialist() {
    this.dataService.getAll("catcommon?maxResultCount=500").subscribe(resp => this.specialist = resp.items);
  }

  _filter(name: string): IHealthfacilities[] {
    const filterValue = name.toLowerCase();
    return this._healthfacilities.filter(h => h.name.toLowerCase().indexOf(filterValue) === 0);
  }

  filterOptions() {
    this.filteredHealthFacilitiesOptions = this.healthfacilities.valueChanges
      .pipe(
        startWith<string | IHealthfacilities>(''),
        map(value => typeof value === 'string' ? value : value.name),
        map(name => name ? this._filter(name) : this._healthfacilities.slice()),
        map(data => data)
      );
  }

  clickCbo() {
    if (!this.healthfacilities.value && this.checkProvince) {
      this.filterOptions();
    }
  }

  displayFn(h?: IHealthfacilities): string | undefined {
    return h ? h.name : undefined;
  }

  openDialogDoctor(obj?: EntityDto): void {
    const dialogRef = this.dialog.open(this.dialogComponent, { minWidth: 'calc(100vw/1.5)', maxWidth: 'calc(100vw - 100px)', disableClose: true, data: obj ? obj : null });
    dialogRef.afterClosed().subscribe(() => {
      this.paginator.pageIndex = 0;
      this.paginator._changePageSize(this.paginator.pageSize);
    });
  }
}