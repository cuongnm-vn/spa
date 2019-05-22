import { AfterViewInit, Component, Injector, OnInit, ViewEncapsulation } from '@angular/core';
import { FormBuilder, FormGroup, Validators, FormControl } from '@angular/forms';
import { HttpClient, HttpHeaders } from '@angular/common/http';

import { AbpSessionService } from '@abp/session/abp-session.service';
import { AppComponentBase } from '@shared/app-component-base';
import { LoginService } from './login.service';
import { Router } from '@angular/router';
import { IHealthfacilities } from '@shared/Interfaces';
import { debounceTime, tap, switchMap, finalize } from 'rxjs/operators';
import { DataService } from '@shared/service-proxies/service-data';
import { DomSanitizer } from '@angular/platform-browser';

@Component({
    selector: 'app-login',
    templateUrl: './login.component.html',
    styleUrls: ['./login.component.scss'],
    encapsulation: ViewEncapsulation.None
})
export class LoginComponent extends AppComponentBase implements OnInit {

    frmLogin: FormGroup;
    submitted = false;
    saving = true;
    isLoading = false;
    healthfacilities = new FormControl();
    dataService: DataService;
    
    _healthfacilities = [];
    numberLoginFail = 0;

    constructor(private _sanitizer: DomSanitizer, private _dataService: DataService, private http: HttpClient, injector: Injector, public loginService: LoginService, private _formBuilder: FormBuilder, private _router: Router, private _sessionService: AbpSessionService) {
        super(injector);
    }

    ngOnInit(): void {
        this.frmLogin = this._formBuilder.group({
            userNameOrEmailAddress: [this.loginService.authenticateModel.userNameOrEmailAddress, Validators.required],
            password: [this.loginService.authenticateModel.password, Validators.required],
            healthfacilities: [],
            codeCapcha: [''],
        }); 
        this.dataService = this._dataService;
        this.filterOptions();
        this.healthfacilities.setValue(null);

        console.log(122, localStorage.getItem('logCount'));
        if (localStorage.getItem('logCount') != null) {
            this.numberLoginFail = parseInt(localStorage.getItem('logCount'));
        }
        this.getCapcha();
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
    

    login(): void {
        let numLoginFail = 0;
        if (localStorage.getItem('logCount') != null) {
            numLoginFail = parseInt(localStorage.getItem('logCount'));
        }

        if (numLoginFail > 5) {
            if (this.frmLogin.controls['codeCapcha'].value != this._capcha.code) {
                this.capcha = true;
                return;
            }
        }

        this.submitted = true;
        if (this.frmLogin.invalid) { return; }
        // this.submitting = true;

        this.loginService.authenticateModel = Object.assign(this.loginService.authenticateModel, this.frmLogin.value);
        this.loginService.authenticate(() => { }, error => {
                localStorage.setItem('logCount', (numLoginFail + 1).toString())
            });
    }

    getCapcha() {
        this._dataService.getAny('get-captcha-image').subscribe(res => this._capcha = { code: res.code, data: this._sanitizer.bypassSecurityTrustResourceUrl('data:image/jpg;base64,' + res.data) });
    }

    validateCapcha(value: any) {
        if (value.length == 4) this._capcha.code != value ? this.capcha = true : this.capcha = false;
    }

    //add h
    displayFn(h?: IHealthfacilities): string | undefined {
        return h ? h.name : undefined;
    }

    filterOptions() {
        this.healthfacilities.valueChanges
            .pipe(
                debounceTime(500),
                tap(() => this.isLoading = true),
                switchMap(value => this.filter(value))
            )
            .subscribe(data => {
                this._healthfacilities = data.items;
            });
    }

    filter(value: any) {
        var fValue = typeof value === 'string' ? value : (value ? value.name : '')
        this._healthfacilities = [];

        return this.dataService
            .get("healthfacilities", JSON.stringify({
                name: isNaN(fValue) ? fValue : "",
                code: !isNaN(fValue) ? fValue : ""
            }), '', null, null)
            .pipe(
                finalize(() => this.isLoading = false)
            )
    }

    focusoutHealthfacilities(){
        !this.healthfacilities.value ? this.healthfacilities.setErrors({required: true}) : "";
    }

    onSelectHealthFacilities(obj: any) {
        this.frmLogin.controls['healthfacilities'].setValue(obj.healthFacilitiesId);
    }
}
