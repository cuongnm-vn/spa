import * as _ from 'lodash';

import { ActivatedRouteSnapshot, ActivationEnd, NavigationEnd, NavigationError, NavigationStart, Router } from '@angular/router';
import { AfterViewInit, ChangeDetectorRef, Component, Injector, OnInit, ViewContainerRef, ViewEncapsulation } from '@angular/core';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { filter, map } from 'rxjs/operators';

import { AppAuthService } from '@shared/auth/app-auth.service';
import { AppComponentBase } from '@shared/app-component-base';
import { Observable } from 'rxjs';
import { SignalRAspNetCoreHelper } from '@shared/helpers/SignalRAspNetCoreHelper';
import { Title } from '@angular/platform-browser';
import { MAT_DIALOG_DATA, MatButton, MatDialog, MatDialogRef } from '@angular/material';
import { TaskComponent } from './reset-pasword/task/task.component';
//import { PagedListingComponentBase } from '@shared/paged-listing-component-base';
//import { TaskComponent } from './sms-template-task/task/task.component';

@Component({
    templateUrl: './app.component.html',
    styleUrls: ['./app.component.scss'],
    encapsulation: ViewEncapsulation.None
})
export class AppComponent extends AppComponentBase implements OnInit, AfterViewInit {

    private viewContainerRef: ViewContainerRef;
    private title = 'Viettel Gateway';
    public pateTitle = '';
    dialogComponent: any;

    isHandset$: Observable<boolean> = this.breakpointObserver.observe(Breakpoints.Handset).pipe(map(result => result.matches));
    shownLoginName: string = '';

    languages: abp.localization.ILanguageInfo[];
    currentLanguage: abp.localization.ILanguageInfo;

    constructor(injector: Injector, private breakpointObserver: BreakpointObserver, private _authService: AppAuthService, private router: Router, private titleService: Title, public dialog: MatDialog) {
        super(injector);
        this.shownLoginName = this.appSession.getShownLoginName();
        this.router.events.subscribe((event: any) => {
            if (event instanceof NavigationStart) {
                // Show loading indicator
                this.isTableLoading = true;
            }
 
            if (event instanceof NavigationEnd) {
                // Hide loading indicator
                //const menu = abp.nav.menus['mainMenu'].items.find((e: any) => e.route === event.url);
 
                var menu;
                abp.nav.menus['mainMenu'].items.forEach(el => {
                    if(el.route === this.router.url && el.items.length == 0){
                        menu = el;
                    } else{
                        if (el.items.length > 0) {
                            el.items.forEach(eli => {
                                if(eli.route + '/index' === this.router.url) menu = eli;
                                if(eli.route + '/packageindex' === this.router.url) menu = eli;
                            });
                        }
                    }
                });
 
                if (menu) {
                    this.titleService.setTitle(`${this.title} | ${this.l(menu.name)}`);
                    this.pateTitle = this.l(menu.name);
                }
 
                this.isTableLoading = false;
            }
 
            if (event instanceof NavigationError) {
                // Hide loading indicator
 
                // Present error to user
            }
        });
    }

    ngOnInit(): void {
        // SignalRAspNetCoreHelper.initSignalR();
        this.dialogComponent = TaskComponent;

        this.languages = _.filter(this.localization.languages, l => (<any>l).isDisabled === false);
        this.currentLanguage = this.localization.currentLanguage;

        abp.event.on('abp.notifications.received', userNotification => {
            abp.notifications.showUiNotifyForUserNotification(userNotification);

            // Desktop notification
            // Push.create('AbpZeroTemplate', {
            //     body: userNotification.notification.data.message,
            //     icon: abp.appPath + 'assets/app-logo-small.png',
            //     timeout: 6000,
            //     onClick: function() {
            //         window.focus();
            //         this.close();
            //     }
            // });
        });
    }

    changeLanguage(language: abp.localization.ILanguageInfo) {
        abp.utils.setCookieValue(
            'Abp.Localization.CultureName',
            language.name,
            new Date(new Date().getTime() + 5 * 365 * 86400000), // 5 year
            abp.appPath
        );

        location.reload();
    }

    ngAfterViewInit(): void {
    }

    logout(): void {
        localStorage.removeItem('logCount');
        this._authService.logout();
    }

    onResize(event) {
        // exported from $.AdminBSB.activateAll
    }

    openCustomDialog(): void {
        const dialogRef = this.dialog.open(this.dialogComponent, { minWidth: '400px', maxWidth: '400px'});
        dialogRef.afterClosed();
    }
}
