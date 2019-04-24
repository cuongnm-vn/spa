import { Component, Injector, OnInit, ViewChild } from '@angular/core';
import { IBookingInformations } from '@shared/Interfaces';
import { MatDialog } from '@angular/material';

import { DataService } from '@shared/service-proxies/service-data';
import { FormBuilder } from '@angular/forms';
import { PagedListingComponentBase } from '@shared/paged-listing-component-base';
import { TaskComponent } from '../task/task.component';
import { DetailComponent } from '../detail/detail.component';
import swal from 'sweetalert2';
import { MomentDateAdapter } from '@angular/material-moment-adapter';
import * as moment from 'moment';
import { DateAdapter, MAT_DATE_FORMATS, MAT_DATE_LOCALE } from '@angular/material/core';

export const MY_FORMATS = {
    parse: {
        dateInput: 'DD/MM/YYYY',
    },
    display: {
        dateInput: 'DD/MM/YYYY',
        monthYearLabel: 'MMM YYYY',
        dateA11yLabel: 'LL',
        monthYearA11yLabel: 'MMMM YYYY',
    },
};

@Component({
    selector: 'app-index',
    templateUrl: './index.component.html',
    styleUrls: ['./index.component.scss'],
    providers: [
        { provide: DateAdapter, useClass: MomentDateAdapter, deps: [MAT_DATE_LOCALE] },
        { provide: MAT_DATE_FORMATS, useValue: MY_FORMATS },
    ],
})
export class IndexComponent extends PagedListingComponentBase<IBookingInformations> implements OnInit {

    displayedColumns = ['orderNumber', 'code', 'patient', 'gender', 'phone', 'year', 'description', 'doctor', 'examinationDate', 'status', 'task'];
    status = [{ id: 4, name: 'Tất cả' }, { id: 3, name: 'Hủy khám' }, { id: 2, name: 'Đã khám' }, { id: 1, name: 'Chưa khám' }, { id: 0, name: 'Mới đăng ký' }];
    times = [{ id: 10, name: 'Theo khoảng thời gian' }, { id: 9, name: 'Năm trước' }, { id: 8, name: 'Năm nay' }, { id: 7, name: 'Quý trước' }, { id: 6, name: 'Quý này' },
        { id: 5, name: 'Tháng trước' }, { id: 4, name: 'Tháng này' }, { id: 3, name: 'Tuần trước' }, { id: 2, name: 'Tuần nay' }, { id: 1, name: 'Hôm qua' }, { id: 0, name: 'Hôm nay' }];
    dialogDetail: any;
    _medicalFacility = [];
    _doctors = [];
    _isChosenTime = false;

    @ViewChild("startTime") startTime;
    @ViewChild("endTime") endTime;

    constructor(injector: Injector, private _dataService: DataService, public dialog: MatDialog, private _formBuilder: FormBuilder) {
        super(injector);
    }

    ngOnInit() {
        this.api = 'bookinginformations';
        this.frmSearch = this._formBuilder.group({ healthfacilities: [], doctor: [], packagesNameDescription: [], status: [], startTime: [], endTime: [] });
        this.dataService = this._dataService;
        this.dialogComponent = TaskComponent;
        this.dialogDetail = DetailComponent;
        this.dataService.getAll('healthfacilities', (this.appSession.user.healthFacilitiesId ? String(this.appSession.user.healthFacilitiesId) : '')).subscribe(resp => { this._medicalFacility = resp.items });
        this.dataService.getAll('doctors').subscribe(resp => { this._doctors = resp.items });

        setTimeout(() => {
            this.startTime.nativeElement.value = moment(new Date()).format("DD/MM/YYYY");
            this.endTime.nativeElement.value = moment(new Date()).format("DD/MM/YYYY");
        });
    }

    getGender(status: number) {
        switch (status) {
            case 0:
                return 'Không xác định';
            case 1:
                return 'Nam';
            case 2:
                return 'Nữ';
        }
    }

    updateTimeToSearch() {
        this.frmSearch.controls['startTime'].setValue(moment(this.startTime.nativeElement.value, 'DD/MM/YYYY').add(1, 'day').toDate());

        this.frmSearch.controls['endTime'].setValue(moment(this.endTime.nativeElement.value, 'DD/MM/YYYY').add(1, 'day').toDate());
    }

    handleTime(element: IBookingInformations) {
        if (element.doctorId == null) {
            switch (element.examinationWorkingTime) {
                case 1:
                    return '8:00 - 12:00';
                case 2:
                    return '12:00 - 17:00';
                case 3:
                    return '17:00 - 21:00';
            }
        } else {
            if (element.timeSlotId != null) {
                return element.bookingTimeSlot.hoursStart + ":" + element.bookingTimeSlot.minuteStart + " - " + element.bookingTimeSlot.hoursEnd + ":" + element.bookingTimeSlot.minuteEnd;
            } else {
                return element.examinationTime;
            }
        }
    }

    getStatus(status: number) {
        switch (status) {
            case 0:
                return 'Mới đăng ký';
            case 1:
                return 'Chưa khám';
            case 2:
                return 'Đã khám';
            case 3:
                return 'Hủy khám';
            case 4:
                return 'Tất cả';
                
        }
    }

    onSelectTime(type: number) {
        switch (type) {
            case 0:
                this.startTime.nativeElement.value = moment(new Date()).format("DD/MM/YYYY");
                this.endTime.nativeElement.value = moment(new Date()).format("DD/MM/YYYY");
                break;
            case 1:
                this.startTime.nativeElement.value = moment(new Date()).add(-1, 'day').format("DD/MM/YYYY");
                this.endTime.nativeElement.value = moment(new Date()).format("DD/MM/YYYY");
                break;
            case 2:
                this.startTime.nativeElement.value = moment(new Date()).startOf('week').format("DD/MM/YYYY");
                this.endTime.nativeElement.value = moment(new Date()).endOf('week').format("DD/MM/YYYY");
                break;
            case 3:
                this.startTime.nativeElement.value = moment(new Date()).add(-1, 'week').startOf('week').format("DD/MM/YYYY");
                this.endTime.nativeElement.value = moment(new Date()).add(-1, 'week').endOf('week').format("DD/MM/YYYY");
                break;
            case 4:
                this.startTime.nativeElement.value = moment(new Date()).startOf('month').format("DD/MM/YYYY");
                this.endTime.nativeElement.value = moment(new Date()).endOf('month').format("DD/MM/YYYY");
                break;
            case 5:
                this.startTime.nativeElement.value = moment(new Date()).add(-1, 'month').startOf('month').format("DD/MM/YYYY");
                this.endTime.nativeElement.value = moment(new Date()).add(-1, 'month').endOf('month').format("DD/MM/YYYY");
                break;
            case 6:
                var quarterAdjustment = (moment().month() % 3) + 1;
                console.log(quarterAdjustment);
                var lastQuarterEndDate = moment().subtract({ months: quarterAdjustment }).endOf('month');
                this.endTime.nativeElement.value = lastQuarterEndDate.format("DD/MM/YYYY");
                this.startTime.nativeElement.value = lastQuarterEndDate.clone().subtract({ months: 2 }).startOf('month').format("DD/MM/YYYY");
                break;
            case 7:
                var quarterAdjustment = (moment().month() % 3) + 4;
                console.log(quarterAdjustment);
                var lastQuarterEndDate = moment().subtract({ months: quarterAdjustment }).endOf('month');
                this.endTime.nativeElement.value = lastQuarterEndDate.format("DD/MM/YYYY");
                this.startTime.nativeElement.value = lastQuarterEndDate.clone().subtract({ months: 2 }).startOf('month').format("DD/MM/YYYY");
                break;
            case 8:
                this.startTime.nativeElement.value = moment(new Date()).startOf('year').format("DD/MM/YYYY");
                this.endTime.nativeElement.value = moment(new Date()).endOf('year').format("DD/MM/YYYY");
                break;
            case 9:
                this.startTime.nativeElement.value = moment(new Date()).add(-1, 'year').startOf('year').format("DD/MM/YYYY");
                this.endTime.nativeElement.value = moment(new Date()).add(-1, 'year').endOf('year').format("DD/MM/YYYY");
                break;
            case 10:
                break;
        }
        this.updateTimeToSearch();
    }
    
    detail(obj): void{
        const dialogRef = this.dialog.open(this.dialogDetail, { minWidth: 'calc(100vw/2)', maxWidth: 'calc(100vw - 300px)', data: obj ? obj : null });
        dialogRef.afterClosed().subscribe(() => this.paginator._changePageSize(this.paginator.pageSize));
    }

    showMessage(title: string, content: string, type: string){
        swal(this.l('PackagesMessageTitle.'), this.l('PackagesMessageContent'), 'error');
    }
}