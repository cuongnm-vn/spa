import { HttpClient, HttpHeaders, HttpResponse, HttpResponseBase } from '@angular/common/http';
import { Inject, Injectable, InjectionToken, Optional } from '@angular/core';
import { Observable, from as _observableFrom, of as _observableOf, throwError as _observableThrow } from 'rxjs';
import { catchError as _observableCatch, mergeMap as _observableMergeMap } from 'rxjs/operators';

import { AppConsts } from '@shared/AppConsts';

export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL');

@Injectable()
export class DataService {

    private http: HttpClient;
    private baseUrl: string;
    protected jsonParseReviver: ((key: string, value: any) => any) | undefined = undefined;

    constructor(@Inject(HttpClient) http: HttpClient, @Optional() @Inject(API_BASE_URL) baseUrl?: string) {
        this.http = http;
        this.baseUrl = AppConsts.remoteServiceBaseUrl;
    }

    getAll(enpoint: string, filter?: string | null | undefined, sorting?: string | null | undefined): Observable<any> {
        let url_ = this.baseUrl + `/${enpoint}?`;
        if (filter !== undefined) { url_ += 'Filter=' + encodeURIComponent('' + filter) + '&'; }
        if (sorting !== undefined) { url_ += 'Sorting=' + encodeURIComponent('' + sorting) + '&'; }
        url_ = url_.replace(/[?&]$/, '');

        const options_: any = { observe: 'response', responseType: 'blob', headers: new HttpHeaders({ 'Content-Type': 'application/json', 'Accept': 'application/json' }) };

        return this.http.request('get', url_, options_).pipe(_observableMergeMap((response_: any) => this.processDataOk(response_)))
            .pipe(_observableCatch((response_: any) => {
                if (response_ instanceof HttpResponseBase) {
                    try {
                        return this.processDataOk(<any>response_);
                    } catch (e) {
                        return <Observable<any>><any>_observableThrow(e);
                    }
                } else {
                    return <Observable<any>><any>_observableThrow(response_);
                }
            }));
    }

    /**
     * @filter (optional)
     * @sorting (optional)
     * @skipCount (optional)
     * @maxResultCount (optional)
     * @return Success
     */
    get(enpoint: string, filter: object | string | null | undefined, sorting: string | null | undefined, skipCount: number | null | undefined, maxResultCount: number | null | undefined): Observable<any> {
        let url_ = this.baseUrl + `/${enpoint}?`;

        if (filter !== undefined) { url_ += 'Filter=' + encodeURIComponent('' + filter) + '&'; }
        if (sorting !== undefined) { url_ += 'Sorting=' + encodeURIComponent('' + sorting) + '&'; }
        if (skipCount !== undefined) { url_ += 'SkipCount=' + encodeURIComponent('' + skipCount) + '&'; }
        if (maxResultCount !== undefined) { url_ += 'MaxResultCount=' + encodeURIComponent('' + maxResultCount) + '&'; }
        url_ = url_.replace(/[?&]$/, '');
        const options_: any = { observe: 'response', responseType: 'blob', headers: new HttpHeaders({ 'Content-Type': 'application/json', 'Accept': 'application/json' }) };
        abp.ui.setBusy('#main-container');
        return this.http.request('get', url_, options_).pipe(_observableMergeMap((response_: any) => this.processDataOk(response_)))
            .pipe(_observableCatch((response_: any) => {
                abp.ui.clearBusy('#main-container');
                if (response_ instanceof HttpResponseBase) {
                    try {
                        return this.processDataOk(<any>response_);
                    } catch (e) {
                        return <Observable<any>><any>_observableThrow(e);
                    }
                } else {
                    return <Observable<any>><any>_observableThrow(response_);
                }
            }));
    }

    getAny(enpoint: string): Observable<any> {
        let url_ = this.baseUrl + `/${enpoint}?`;
        url_ = url_.replace(/[?&]$/, '');

        const options_: any = { observe: 'response', responseType: 'blob', headers: new HttpHeaders({ 'Content-Type': 'application/json', 'Accept': 'application/json' }) };

        return this.http.request('get', url_, options_).pipe(_observableMergeMap((response_: any) => this.processDataOk(response_)))
            .pipe(_observableCatch((response_: any) => {
                if (response_ instanceof HttpResponseBase) {
                    try {
                        return this.processDataOk(<any>response_);
                    } catch (e) {
                        return <Observable<any>><any>_observableThrow(e);
                    }
                } else {
                    return <Observable<any>><any>_observableThrow(response_);
                }
            }));
    }

    /**
     * @input (optional)
     * @return Success
     */

    createUpload(enpoint: string, input: any | null | undefined): Observable<any> {
        let url_ = this.baseUrl + `/${enpoint}`;
        url_ = url_.replace(/[?&]$/, '');
        var specialist = "";
        var healhFacilities = "";
        const formData: FormData = new FormData();
        // tslint:disable-next-line: forin
        for (const key in input) {
            if (key === 'avatar') {
                if (input.avatar) {
                    formData.append('avatar', input.avatar, input.avatar.name);
                }
            }
            if (key === 'cmnd') {
                if (input.avatar) {
                    formData.append('cmnd', input.avatar, input.avatar.name);
                }
            }
            if (key === 'gp') {
                if (input.avatar) {
                    formData.append('gp', input.avatar, input.avatar.name);
                }
            }
            if (key === 'specialist') {
                Array.from(input.specialist).forEach((e: any) => specialist += e.specialistCode + ",");
            }
            if (key === 'healthfacilities') {
                Array.from(input.healthfacilities).forEach((h: any) => healhFacilities += h.healthFacilitiesId + ",");
            }
            else {
                formData.append(key, input[key]);
            }
        }


        formData.append('specials', specialist);
        formData.append('healths', healhFacilities);
        const options_: any = {
            body: formData,
            observe: 'response',
            responseType: 'blob',
            headers: new HttpHeaders({
                'Accept': 'application/json'
            })
        };


        abp.ui.setBusy('#form-dialog');
        return this.http.request('post', url_, options_).pipe(_observableMergeMap((response_: any) => {
            return this.processDataOk(response_);
        })).pipe(_observableCatch((response_: any) => {
            abp.ui.clearBusy('#form-dialog');
            if (response_ instanceof HttpResponseBase) {
                try {
                    return this.processDataOk(<any>response_);
                } catch (e) {
                    return <Observable<any>><any>_observableThrow(e);
                }
            } else {
                return <Observable<any>><any>_observableThrow(response_);
            }
        }));
    }

    updateUpload(enpoint: string, input: any | null | undefined): Observable<any> {
        let url_ = this.baseUrl + `/${enpoint}`;
        url_ = url_.replace(/[?&]$/, '');
        var specialist = "";
        var healhFacilities = "";
        const formData: FormData = new FormData();

        // tslint:disable-next-line: forin
        // for (const key in input) {
        //     if (key === 'avatars') {
        //         if (input.files && input.files.files && input.files.files.length) {
        //             Array.from(input.files.files).forEach((f: any) => formData.append('avatars', f));
        //         }
        //     } else {
        //         Array.isArray(input[key])
        //             ? formData.append(key, JSON.stringify(input[key]))
        //             : formData.append(key, input[key]);
        //         // formData.append(key, input[key]);
        //     }
        // }

        for (const key in input) {
            if (key === 'avatar') {
                if (input.avatar) {
                    formData.append('avatar', input.avatar, input.avatar.name);
                }
            }
            if (key === 'specialist') {
                Array.from(input.specialist).forEach((e: any) => specialist += e.specialistCode + ",");
            }
            if (key === 'healthfacilities') {
                Array.from(input.healthfacilities).forEach((h: any) => healhFacilities += h.healthFacilitiesId + ",");
            }
            else {
                formData.append(key, input[key]);
            }
        }

        formData.append('specials', specialist);
        formData.append('healths', healhFacilities);
        const options_: any = {
            body: formData,
            observe: 'response',
            responseType: 'blob',
            headers: new HttpHeaders({ 'Accept': 'application/json' })
        };
        abp.ui.setBusy('#form-dialog');
        return this.http.request('put', url_, options_).pipe(_observableMergeMap((response_: any) => {
            return this.processDataOk(response_);
        })).pipe(_observableCatch((response_: any) => {
            abp.ui.clearBusy('#form-dialog');
            if (response_ instanceof HttpResponseBase) {
                try {
                    return this.processDataOk(<any>response_);
                } catch (e) {
                    return <Observable<any>><any>_observableThrow(e);
                }
            } else {
                return <Observable<any>><any>_observableThrow(response_);
            }
        }));
    }

    create(enpoint: string, input: any | null | undefined): Observable<any> {
        let url_ = this.baseUrl + `/${enpoint}`;
        url_ = url_.replace(/[?&]$/, '');

        const content_ = JSON.stringify(input);
        const options_: any = {
            body: content_,
            observe: 'response',
            responseType: 'blob',
            headers: new HttpHeaders({
                'Content-Type': 'application/json',
                'Accept': 'application/json'
            })
        };

        abp.ui.setBusy('#form-dialog');
        return this.http.request('post', url_, options_).pipe(_observableMergeMap((response_: any) => {
            return this.processDataOk(response_);
        })).pipe(_observableCatch((response_: any) => {

            abp.ui.clearBusy('#form-dialog');
            if (response_ instanceof HttpResponseBase) {
                try {
                    return this.processDataOk(<any>response_);
                } catch (e) {
                    return <Observable<any>><any>_observableThrow(e);
                }
            } else {
                return <Observable<any>><any>_observableThrow(response_);
            }
        }));
    }


    createUser(enpoint: string, input: any | null | undefined): Observable<any> {
        let url_ = this.baseUrl + `/${enpoint}`;
        url_ = url_.replace(/[?&]$/, '');

        let groupId = '';
        let healthId  = '';
        
        const formData: FormData = new FormData();
        for (const key in input) {
            if (key === 'cmnd') {
                if (input.cmnd && input.cmnd.files && input.cmnd.files.length) {
                    Array.from(input.cmnd.files).forEach((f: any) => formData.append('cmnd', f));
                }
            }
            if (key === 'gp') {
                if (input.gp && input.gp.files && input.gp.files.length) {
                    Array.from(input.gp.files).forEach((f: any) => formData.append('gp', f));
                }
            }
            if (key === 'groupUser') {
                Array.from(input.groupUser).forEach((h: any) => {
                    groupId += h.groupId + '-' + h.applicationId + ',';
                });
            }
            if (key === 'healthId') {
                Array.from(input.healthId).forEach((h: any) => {
                    healthId += h.healthFacilitiesId + ',';
                });
            }
            else {
                formData.append(key, input[key]);
            }
        }
        
        formData.append('groupId', groupId);
        formData.append('healthId', healthId);
        const options_: any = {
            body: formData,
            observe: 'response',
            responseType: 'blob',
            headers: new HttpHeaders({
                'Accept': 'application/json'
            })
        };
       
        return this.http.request('post', url_, options_).pipe(_observableMergeMap((response_: any) => {
            return this.processDataOk(response_);
        })).pipe(_observableCatch((response_: any) => {
            if (response_ instanceof HttpResponseBase) {
                try {
                    return this.processDataOk(<any>response_);
                } catch (e) {
                    return <Observable<any>><any>_observableThrow(e);
                }
            } else {
                return <Observable<any>><any>_observableThrow(response_);
            }
        }));
    }
    
    updateUser(enpoint: string, input: any | null | undefined): Observable<any> {
        let url_ = this.baseUrl + `/${enpoint}`;
        url_ = url_.replace(/[?&]$/, '');

        let groupId = '';
        let healthId = '';
        console.log(2009, input);
        const formData: FormData = new FormData();
        for (const key in input) {
            if (key === 'gp') {
                if (input.gp && input.gp.files && input.gp.files.length) {
                    Array.from(input.gp.files).forEach((f: any) => formData.append('gp', f));
                }
            }
            if (key === 'cmnd') {
                if (input.cmnd && input.cmnd.files && input.cmnd.files.length !== 0) {
                    Array.from(input.cmnd.files).forEach((f: any) => formData.append('cmnd', f));
                }
            }
            if (key === 'groupUser') {
                Array.from(input.groupUser).forEach((h: any) => {
                    groupId += h.groupId + '-' + h.applicationId + ',';
                });
            }
            if (key === 'healthId') {
                Array.from(input.healthId).forEach((h: any) => {
                    healthId += h.healthFacilitiesId + ',';
                });
            }
            else {
                formData.append(key, input[key]);
            }
        }
        
        formData.append('groupId', groupId);
        formData.append('healthId', healthId);
        const options_: any = {
            body: formData,
            observe: 'response',
            responseType: 'blob',
            headers: new HttpHeaders({ 'Accept': 'application/json' })
        };

        abp.ui.setBusy('#form-dialog');
        return this.http.request('put', url_, options_).pipe(_observableMergeMap((response_: any) => {
            return this.processDataOk(response_);
        })).pipe(_observableCatch((response_: any) => {
            abp.ui.clearBusy('#form-dialog');
            if (response_ instanceof HttpResponseBase) {
                try {
                    return this.processDataOk(<any>response_);
                } catch (e) {
                    return <Observable<any>><any>_observableThrow(e);
                }
            } else {
                return <Observable<any>><any>_observableThrow(response_);
            }
        }));
    }

    /**
     * @input (optional)
     * @return Success
     */
    update(enpoint: string, input: any | null | undefined): Observable<any> {
        let url_ = this.baseUrl + `/${enpoint}`;
        url_ = url_.replace(/[?&]$/, '');

        const content_ = JSON.stringify(input);

        const options_: any = {
            body: content_,
            observe: 'response',
            responseType: 'blob',
            headers: new HttpHeaders({
                'Content-Type': 'application/json',
                'Accept': 'application/json'
            })
        };

        abp.ui.setBusy('#form-dialog');
        return this.http.request('put', url_, options_).pipe(_observableMergeMap((response_: any) => {
            return this.processDataOk(response_);
        })).pipe(_observableCatch((response_: any) => {

            abp.ui.clearBusy('#form-dialog');
            if (response_ instanceof HttpResponseBase) {
                try {
                    return this.processDataOk(<any>response_);
                } catch (e) {
                    return <Observable<any>><any>_observableThrow(e);
                }
            } else {
                return <Observable<any>><any>_observableThrow(response_);
            }
        }));
    }

    updateMini(enpoint: string, input: any | null | undefined): Observable<any> {
        let url_ = this.baseUrl + `/${enpoint}`;
        url_ = url_.replace(/[?&]$/, '');
        const formData: FormData = new FormData();

        for (const key in input) {
            formData.append(key, input[key]);
        }
        const options_: any = {
            body: formData,
            observe: 'response',
            responseType: 'blob',
            headers: new HttpHeaders({ 'Accept': 'application/json' })
        };

        return this.http.request('put', url_, options_).pipe(_observableMergeMap((response_: any) => {
            return this.processDataOk(response_);
        })).pipe(_observableCatch((response_: any) => {
            if (response_ instanceof HttpResponseBase) {
                try {
                    return this.processDataOk(<any>response_);
                } catch (e) {
                    return <Observable<any>><any>_observableThrow(e);
                }
            } else {
                return <Observable<any>><any>_observableThrow(response_);
            }
        }));
    }

    /**
    * @id (optional)
    * @return Success
    */
    delete(enpoint: string, id: number | null | undefined): Observable<void> {
        let url_ = this.baseUrl + `/${enpoint}?`;
        if (id !== undefined) {
            url_ += 'Id=' + encodeURIComponent('' + id) + '&';
        }
        url_ = url_.replace(/[?&]$/, '');

        const options_: any = {
            observe: 'response',
            responseType: 'blob',
            headers: new HttpHeaders({
                'Content-Type': 'application/json',
            })
        };

        abp.ui.setBusy('#form-dialog');
        return this.http.request('delete', url_, options_).pipe(_observableMergeMap((response_: any) => this.processDataOk(response_))).pipe(_observableCatch((response_: any) => {

            abp.ui.clearBusy('#form-dialog');
            if (response_ instanceof HttpResponseBase) {
                try {
                    return this.processDataOk(<any>response_);
                } catch (e) {
                    return <Observable<void>><any>_observableThrow(e);
                }
            } else {
                return <Observable<void>><any>_observableThrow(response_);
            }
        }));
    }

    protected processDataOk(response: HttpResponseBase): Observable<any> {
        abp.ui.clearBusy('#main-container');
        const status = response.status;
        const responseBlob = response instanceof HttpResponse ? response.body : (<any>response).error instanceof Blob ? (<any>response).error : undefined;

        const _headers: any = {}; if (response.headers) { for (const key of response.headers.keys()) { _headers[key] = response.headers.get(key); } };

        if (status === 200) {
            return blobResponseToText(responseBlob).pipe(_observableMergeMap(_responseText => _observableOf(_responseText === '' ? null : JSON.parse(_responseText, this.jsonParseReviver))));
        } else if (status !== 200 && status !== 204) {
            return blobResponseToText(responseBlob).pipe(_observableMergeMap(_responseText => {
                return throwException('An unexpected server error occurred.', status, _responseText, _headers);
            }));
        }
        return _observableOf<any>(<any>null);
    }


}

function throwException(message: string, status: number, response: string, headers: { [key: string]: any; }, result?: any): Observable<any> {
    if (result !== null && result !== undefined) {
        return _observableThrow(result);
    } else {
        return _observableThrow(new SwaggerException(message, status, response, headers, null));
    }
}

function blobResponseToText(blob: any): Observable<string> {
    return new Observable<string>((observer: any) => {
        if (!blob) {
            observer.next('');
            observer.complete();
        } else {
            const reader = new FileReader();
            reader.onload = function () {
                observer.next(this.result);
                observer.complete();
            }
            reader.readAsText(blob);
        }
    });
}

export class SwaggerException extends Error {
    message: string;
    status: number;
    response: string;
    headers: { [key: string]: any; };
    result: any;

    protected isSwaggerException = true;

    static isSwaggerException(obj: any): obj is SwaggerException {
        return obj.isSwaggerException === true;
    }

    constructor(message: string, status: number, response: string, headers: { [key: string]: any; }, result: any) {
        super();

        this.message = message;
        this.status = status;
        this.response = response;
        this.headers = headers;
        this.result = result;
    }
}
