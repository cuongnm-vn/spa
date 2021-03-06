import * as _ from 'lodash';
import { Component, Inject, Injector, OnInit, ViewChild, AfterViewInit, ViewEncapsulation } from '@angular/core';
import { FormBuilder, FormGroup, Validators, FormControl } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef, MatInput } from '@angular/material';
import { AppComponentBase } from '@shared/app-component-base';
import { DataService } from '@shared/service-proxies/service-data';
import { ValidationRule } from '@shared/common/common';

import { HttpClient, HttpHeaders } from '@angular/common/http';

import { AbpSessionService } from '@abp/session/abp-session.service';

import { Router, Route } from '@angular/router';
import { DomSanitizer } from '@angular/platform-browser';
import * as moment from 'moment';
import swal from 'sweetalert2';
import { LoginService } from '../../../auth/login/login.service';
import { notifyToastr } from '@shared/helpers/utils';


@Component({
    selector: 'app-task',
    templateUrl: './task_session.component.html',
    styleUrls: ['./task_session.component.scss'],
    encapsulation: ViewEncapsulation.None
})
export class TaskSessionComponent extends AppComponentBase implements OnInit {
    frmLogin: FormGroup;
    submitted = false;
    saving = true;
    isLoading = false;
    dataService: DataService;
    numberLoginFail = 0;

    @ViewChild('userNameOrEmail') userNameOrEmail;
    @ViewChild('codeCapcha') codeCapcha;

    //contructor
    constructor(
        injector: Injector,
        private _dataService: DataService,
        private _formBuilder: FormBuilder,
        public dialogRef: MatDialogRef<TaskSessionComponent>,
        @Inject(MAT_DIALOG_DATA) public obj,
        private router: Router,
        private _sanitizer: DomSanitizer,
        private http: HttpClient,
        private _sessionService: AbpSessionService,
        public loginService: LoginService
    ) {
        super(injector);
    }

    ngOnInit(): void {
        this.frmLogin = this._formBuilder.group({
            userNameOrEmailAddress: [localStorage.getItem('userName'), Validators.required], //this.loginService.authenticateModel.userNameOrEmailAddress
            password: [localStorage.getItem('password'), Validators.required], //this.loginService.authenticateModel.password
            codeCapcha: [''],
            isRemberMeChecked: [false]
        });
        this.dataService = this._dataService;
        this.getCapcha();

        setTimeout(() => {
            this.userNameOrEmail.nativeElement.focus();
        }, 1000);
    }

    get f() { return this.frmLogin.controls; }
    get multiTenancySideIsTeanant(): boolean {
        return this._sessionService.tenantId > 0;
    }
    get isSelfRegistrationAllowed(): boolean {
        if (!this._sessionService.tenantId) {
            return false;
        }

        return true;
    }

    capcha = false;
    _capcha: { code: string, data: any } = { code: '', data: '' };

    onHandleLoginInput(event) {
        this._dataService.get('auth', JSON.stringify({ 'userName': event.target.value }), null, null, null).subscribe(data => {
            if (data.items != undefined) {
                if (data.items.counter < 10) {
                    this.numberLoginFail = data.items.counter;
                }

                let lockedTime = (moment(Date.now()).valueOf() - moment(new Date(data.items.lockedTime)).valueOf()) / (1000 * 60);
                if (lockedTime >= 0) {
                    if (data.items.counter >= 10) {
                        this._dataService.get('auth', JSON.stringify({ 'userName': this.frmLogin.controls['userNameOrEmailAddress'].value, 'counter': -1 }), null, null, null).subscribe(data => { });
                    }
                }
            }
        });
    }

    login(): void {
        let numLoginFail = 1;
        let lockedTime = 0;

        this._dataService.get('auth', JSON.stringify({ 'userName': this.frmLogin.controls['userNameOrEmailAddress'].value }), null, null, null).subscribe(data => {
            if (!data.items) {
                return notifyToastr(this.l('Notification'), this.l('Đăng nhập không thành công. Tài khoản không tồn tại'), 'warning'); 
                //  swal({
                //     title: this.l('Notification'),
                //     text: this.l('Đăng nhập không thành công. Tài khoản không tồn tại'),
                //     type: 'warning',
                //     timer: 3000
                // });
            }

            if (data.items.status != 2 || data.items.mdmStatus != 1 || moment(new Date(data.items.expriredDate)).valueOf() < moment(Date.now()).valueOf()) {
                this._dataService.get('auth', JSON.stringify({
                    'userName': this.frmLogin.controls['userNameOrEmailAddress'].value
                }), null, null, null).subscribe(data => {
                    return notifyToastr(this.l('Notification'),
                     this.l('Đăng nhập không thành công. Tài khoản chưa được kích hoạt hoặc bị khóa'), 'warning'); 
                    // swal({
                    //     title: this.l('Notification'),
                    //     text: this.l('Đăng nhập không thành công. Tài khoản chưa được kích hoạt hoặc bị khóa'),
                    //     type: 'warning',
                    //     timer: 3000
                    // });
                });
                return;
            }

            lockedTime = (moment(Date.now()).valueOf() - moment(new Date(data.items.lockedTime)).valueOf()) / (1000 * 60);
            if (lockedTime < 0) {
                this.numberLoginFail = 0;
                this.userNameOrEmail.nativeElement.focus();
                this.frmLogin.controls['userNameOrEmailAddress'].setValue('');
                this.frmLogin.controls['password'].setValue('');
                return notifyToastr(this.l('Notification'), 
                this.l('Đăng nhập không thành công. Vui lòng trở lại sau 60 phút'), 'warning'); 
                //  swal({
                //     title: this.l('Notification'),
                //     text: this.l('Đăng nhập không thành công. Vui lòng trở lại sau 60 phút'),
                //     type: 'warning',
                //     timer: 3000
                // });
            }

            numLoginFail = data.items.counter + 1;

            if (numLoginFail >= 10) {
                this.numberLoginFail = 0;
            } else {
                this.numberLoginFail = numLoginFail;
            }

            if (numLoginFail > 5) {
                if (this.frmLogin.controls['codeCapcha'].value != this._capcha.code) {
                    this.capcha = true;
                    this.codeCapcha.nativeElement.focus();
                    this.frmLogin.controls['codeCapcha'].setValue('');
                    this._dataService.get('auth', JSON.stringify({ 'userName': this.frmLogin.controls['userNameOrEmailAddress'].value, 'counter': numLoginFail, 'lockedTime': lockedTime }), null, null, null).subscribe(data => { });
                    if (10 - numLoginFail === 0) {
                        this.userNameOrEmail.nativeElement.focus();
                        this.frmLogin.controls['userNameOrEmailAddress'].setValue('');
                        this.frmLogin.controls['password'].setValue('');
                        return notifyToastr(this.l('Notification'), 
                        this.l('Đăng nhập không thành công. Vui lòng trở lại sau 60 phút'), 'warning');  
                        // swal({
                        //     title: this.l('Notification'),
                        //     text: this.l('Đăng nhập không thành công. Vui lòng trở lại sau 60 phút'),
                        //     type: 'warning',
                        //     timer: 3000
                        // });
                    }
                    else {
                        return notifyToastr(this.l('Notification'), 
                        this.l(`Mã xác nhận không trùng khớp. Bạn còn ${10 - numLoginFail} lần thử`), 'warning');  
                        // swal({
                        //     title: this.l('Notification'),
                        //     text: this.l(`Mã xác nhận không trùng khớp. Bạn còn ${10 - numLoginFail} lần thử`),
                        //     type: 'warning',
                        //     timer: 3000
                        // });
                    }
                }
            }



            this.submitted = true;
            if (this.frmLogin.invalid) { return; }

            if (this.appSession.user.userName != this.frmLogin.controls['userNameOrEmailAddress'].value && this.appSession.user.email != this.frmLogin.controls['userNameOrEmailAddress'].value) {
                localStorage.removeItem('isLoggedIn');
            }

            this.loginService.authenticateModel = Object.assign(this.loginService.authenticateModel, this.frmLogin.value);

            this.loginService.authenticate((success) => {


                if (success) {
                    if (this.frmLogin.controls['isRemberMeChecked'].value) {
                        localStorage.setItem('userName', this.frmLogin.controls['userNameOrEmailAddress'].value);
                        localStorage.setItem('password', this.frmLogin.controls['password'].value);
                    } else {
                        localStorage.removeItem('userName');
                        localStorage.removeItem('password');
                    }
                    this._dataService.get('auth', JSON.stringify({ 'userName': this.frmLogin.controls['userNameOrEmailAddress'].value, 'counter': -1 }), null, null, null).subscribe(() => { this.dialogRef.close();});
                    
                }

            }, error => {
                this._dataService.get('auth', JSON.stringify({
                    'userName': this.frmLogin.controls['userNameOrEmailAddress'].value, 'counter': numLoginFail, 'lockedTime': lockedTime
                }), null, null, null).subscribe(data => {

                    if (numLoginFail > 4) {
                        this.getCapcha();
                    }
                });

                if (numLoginFail <= 5) {
                    return notifyToastr(this.l('Notification'), 
                    this.l(`Đăng nhập không thành công. Tên đăng nhập hoặc mật khẩu không chính xác. Bạn còn ${5 - numLoginFail} lần thử`), 'warning');   
                    // swal({
                    //     title: this.l('Notification'),
                    //     text: this.l(`Đăng nhập không thành công. Tên đăng nhập hoặc mật khẩu không chính xác. Bạn còn ${5 - numLoginFail} lần thử`),
                    //     type: 'warning',
                    //     timer: 3000
                    // });
                }
                else if (10 === numLoginFail) {
                    this.userNameOrEmail.nativeElement.focus();
                    this.frmLogin.controls['userNameOrEmailAddress'].setValue('');
                    this.frmLogin.controls['password'].setValue('');
                    return notifyToastr(this.l('Notification'), 
                    this.l('Đăng nhập không thành công. Vui lòng trở lại sau 60 phút'), 'warning');   
                    // swal({
                    //     title: this.l('Notification'),
                    //     text: this.l('Đăng nhập không thành công. Vui lòng trở lại sau 60 phút'),
                    //     type: 'warning',
                    //     timer: 3000
                    // });
                }
                else {
                    return notifyToastr(this.l('Notification'), 
                    (10 - numLoginFail === 0) ? this.l('Đăng nhập không thành công. Vui lòng trở lại sau 60 phút') 
                    : this.l(`Đăng nhập không thành công. Tên đăng nhập hoặc mật khẩu không chính xác. Bạn còn ${10 - numLoginFail} lần thử`), 'warning');
                    //  swal({
                    //     title: this.l('Notification'),
                    //     text: (10 - numLoginFail === 0) ? this.l('Đăng nhập không thành công. Vui lòng trở lại sau 60 phút') : this.l(`Đăng nhập không thành công. Tên đăng nhập hoặc mật khẩu không chính xác. Bạn còn ${10 - numLoginFail} lần thử`),
                    //     type: 'warning',
                    //     timer: 3000
                    // });
                }
            });
        });
    }

    getCapcha() {
        this._dataService.getAny('get-captcha-image').subscribe(res => this._capcha = { code: res.code, data: this._sanitizer.bypassSecurityTrustResourceUrl('data:image/jpg;base64,' + res.data) });
    }

    validateCapcha(value: any) {
        if (value.length == 4) this._capcha.code != value ? this.capcha = true : this.capcha = false;
    }

    resetPassWordClick() {
        this.router.navigate(['/auth/reset-password']);
    }
}
